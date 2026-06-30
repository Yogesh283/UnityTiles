import uuid
from dataclasses import dataclass
from datetime import datetime, timedelta

from sqlalchemy.orm import Session

from database.models import LeaderboardEntry, RoomPlayer, TournamentResult, TournamentRoom, User
from tournament.broadcast import (
    schedule_countdown,
    schedule_match_finished_broadcast,
    schedule_match_start,
    schedule_player_joined,
    schedule_player_left,
    schedule_room_updated,
)
from tournament.catalog import TournamentDefinition, get_tournament
from tournament.level_selector import generate_room_seed, pick_level_index
from tournament.prize_table import get_paid_rank_count, get_prize
from tournament.ranking import RankedPlayer, rank_players
from tournament.room_state import MATCH_START_COUNTDOWN_SECONDS, serialize_room
from wallet.service import WalletService


@dataclass
class MatchmakeResult:
    room: TournamentRoom


class RoomManager:
    def __init__(self, db: Session):
        self.db = db
        self.wallet = WalletService(db)

    def matchmake(self, tournament_id: str, user_id: int) -> MatchmakeResult:
        tournament = get_tournament(tournament_id)
        if not tournament:
            raise ValueError("Tournament not found")

        existing_room = self._find_user_room(user_id, tournament_id)
        if existing_room:
            return MatchmakeResult(room=existing_room)

        self._cleanup_stale_single_player_rooms(tournament_id, tournament)
        self._cleanup_empty_waiting_rooms(tournament_id)
        self._release_user_from_ended_rooms(user_id, tournament_id)

        existing_room = self._find_user_room(user_id, tournament_id)
        if existing_room:
            return MatchmakeResult(room=existing_room)

        open_room = self._find_open_room(tournament_id, for_update=True)
        if open_room:
            player = self.join_room(open_room, user_id, tournament)
            self._after_player_joined(open_room, player.user_id)
            return MatchmakeResult(room=open_room)

        room = self._create_room(tournament_id, tournament)
        self.join_room(room, user_id, tournament)
        payload = serialize_room(self.db, room)
        schedule_room_updated(room.id, payload)
        return MatchmakeResult(room=room)

    def join_room(
        self,
        room: TournamentRoom,
        user_id: int,
        tournament: TournamentDefinition,
        *,
        skip_fee: bool = False,
    ) -> RoomPlayer:
        existing = (
            self.db.query(RoomPlayer)
            .filter(RoomPlayer.room_id == room.id, RoomPlayer.user_id == user_id)
            .first()
        )
        if existing:
            existing.is_connected = True
            self.db.commit()
            return existing

        count = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).count()
        if count >= room.max_players:
            raise ValueError("Room is full")

        if not skip_fee:
            self.wallet.deduct_entry_fee(user_id, tournament.entry_fee, room.id)

        player = RoomPlayer(room_id=room.id, user_id=user_id, is_connected=True)
        self.db.add(player)
        self.db.commit()
        self.db.refresh(player)

        self._maybe_begin_start_countdown(room, tournament)
        return player

    def set_player_connected(self, room_id: str, user_id: int, connected: bool) -> None:
        player = (
            self.db.query(RoomPlayer)
            .filter(RoomPlayer.room_id == room_id, RoomPlayer.user_id == user_id)
            .first()
        )
        if not player:
            return

        player.is_connected = connected
        self.db.commit()

        room = self.db.query(TournamentRoom).filter(TournamentRoom.id == room_id).first()
        if not room:
            return

        if connected:
            schedule_room_updated(room_id, serialize_room(self.db, room))
            return

        if room.status in {"finished", "locked"}:
            schedule_room_updated(room_id, serialize_room(self.db, room))
            return

        if room.status == "waiting":
            # Keep the player's seat during matchmaking; stale cleanup handles abandoned rooms.
            schedule_room_updated(room_id, serialize_room(self.db, room))
            return

    def tick_rooms(self) -> None:
        rooms = (
            self.db.query(TournamentRoom)
            .filter(TournamentRoom.status.in_(("waiting", "starting")))
            .all()
        )
        for room in rooms:
            tournament = get_tournament(room.tournament_id)
            if not tournament:
                continue

            if room.status == "waiting":
                self._cleanup_stale_single_player_rooms(room.tournament_id, tournament)
                self._maybe_begin_start_countdown(room, tournament)
                schedule_countdown(room.id, serialize_room(self.db, room))
            elif room.status == "starting":
                schedule_countdown(room.id, serialize_room(self.db, room))
                self._maybe_activate_after_countdown(room)

        self._cleanup_finished_rooms()

    def _after_player_joined(self, room: TournamentRoom, user_id: int) -> None:
        self.db.refresh(room)
        payload = serialize_room(self.db, room)
        player_payload = next((p for p in payload["players"] if p["user_id"] == user_id), None)
        if player_payload:
            schedule_player_joined(room.id, player_payload, payload)
        else:
            schedule_room_updated(room.id, payload)
        self._maybe_begin_start_countdown(room, get_tournament(room.tournament_id))

    def _maybe_begin_start_countdown(
        self,
        room: TournamentRoom,
        tournament: TournamentDefinition | None,
    ) -> None:
        if not tournament or room.status != "waiting":
            return

        count = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).count()
        # Duel / small rooms: never start until every seat is filled by real players.
        if room.max_players <= 2 and count < room.max_players:
            return

        if not self.should_start(room, tournament):
            return
        self._begin_start_countdown(room)

    def _begin_start_countdown(self, room: TournamentRoom) -> None:
        if room.status != "waiting":
            return
        room.status = "starting"
        room.started_at = datetime.utcnow()
        self.db.commit()
        schedule_countdown(room.id, serialize_room(self.db, room))

    def _maybe_activate_after_countdown(self, room: TournamentRoom) -> None:
        if room.status != "starting" or not room.started_at:
            return
        deadline = room.started_at + timedelta(seconds=MATCH_START_COUNTDOWN_SECONDS)
        if datetime.utcnow() < deadline:
            return
        self._activate_room(room)

    def _activate_room(self, room: TournamentRoom) -> None:
        if room.status not in {"waiting", "starting"}:
            return
        room.status = "active"
        if not room.started_at:
            room.started_at = datetime.utcnow()
        self.db.commit()
        payload = serialize_room(self.db, room)
        schedule_match_start(room.id, payload)

    def _remove_player_from_waiting_room(self, room: TournamentRoom, player: RoomPlayer) -> None:
        tournament = get_tournament(room.tournament_id)
        if not tournament:
            return

        user_id = player.user_id
        self.wallet.refund_entry_fee(user_id, tournament.entry_fee, room.id)
        self.db.delete(player)
        self.db.commit()

        remaining = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).count()
        if remaining == 0:
            self.db.delete(room)
            self.db.commit()
            schedule_player_left(room.id, user_id, {"room_id": room.id, "status": "closed"})
            return

        payload = serialize_room(self.db, room)
        schedule_player_left(room.id, user_id, payload)

    def _find_user_room(self, user_id: int, tournament_id: str) -> TournamentRoom | None:
        player = (
            self.db.query(RoomPlayer)
            .join(TournamentRoom, TournamentRoom.id == RoomPlayer.room_id)
            .filter(
                RoomPlayer.user_id == user_id,
                TournamentRoom.tournament_id == tournament_id,
                TournamentRoom.status.in_(("waiting", "starting", "active")),
            )
            .order_by(TournamentRoom.created_at.desc())
            .first()
        )
        if not player:
            return None
        return self.db.query(TournamentRoom).filter(TournamentRoom.id == player.room_id).first()

    def _release_user_from_ended_rooms(self, user_id: int, tournament_id: str) -> None:
        """Detach user from locked/finished rooms so new joins can matchmake fresh."""
        stale_players = (
            self.db.query(RoomPlayer)
            .join(TournamentRoom, TournamentRoom.id == RoomPlayer.room_id)
            .filter(
                RoomPlayer.user_id == user_id,
                TournamentRoom.tournament_id == tournament_id,
                TournamentRoom.status.in_(("locked", "finished")),
            )
            .all()
        )
        if not stale_players:
            return

        for player in stale_players:
            room_id = player.room_id
            self.db.delete(player)

        self.db.commit()

        touched_room_ids = {player.room_id for player in stale_players}
        for room_id in touched_room_ids:
            room = self.db.query(TournamentRoom).filter(TournamentRoom.id == room_id).first()
            if not room:
                continue
            remaining = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room_id).count()
            if remaining == 0:
                self.db.delete(room)
        self.db.commit()

    def _cleanup_empty_waiting_rooms(self, tournament_id: str) -> None:
        rooms = (
            self.db.query(TournamentRoom)
            .filter(
                TournamentRoom.tournament_id == tournament_id,
                TournamentRoom.status == "waiting",
            )
            .all()
        )
        for room in rooms:
            count = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).count()
            if count == 0:
                self.db.delete(room)
        self.db.commit()

    def _find_open_room(self, tournament_id: str, *, for_update: bool = False) -> TournamentRoom | None:
        query = (
            self.db.query(TournamentRoom)
            .filter(
                TournamentRoom.tournament_id == tournament_id,
                TournamentRoom.status == "waiting",
            )
            .order_by(TournamentRoom.created_at.asc())
        )
        if for_update:
            query = query.with_for_update()

        rooms = query.all()

        for room in rooms:
            count = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).count()
            if 0 < count < room.max_players:
                return room
        return None

    def _create_room(self, tournament_id: str, tournament: TournamentDefinition) -> TournamentRoom:
        room_id = f"{tournament_id}_{uuid.uuid4().hex[:12]}"
        seed = generate_room_seed(tournament_id, room_id)
        level_index = pick_level_index(seed, tournament)

        room = TournamentRoom(
            id=room_id,
            tournament_id=tournament_id,
            level_index=level_index,
            level_seed=seed,
            status="waiting",
            max_players=tournament.max_players,
        )
        self.db.add(room)
        self.db.commit()
        self.db.refresh(room)
        return room

    def _cleanup_stale_single_player_rooms(
        self,
        tournament_id: str,
        tournament: TournamentDefinition,
    ) -> None:
        rooms = (
            self.db.query(TournamentRoom)
            .filter(
                TournamentRoom.tournament_id == tournament_id,
                TournamentRoom.status == "waiting",
            )
            .all()
        )
        cutoff = datetime.utcnow() - timedelta(seconds=tournament.waiting_seconds)

        for room in rooms:
            players = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).all()
            if len(players) != 1 or room.created_at > cutoff:
                continue

            player = players[0]
            self.wallet.refund_entry_fee(player.user_id, tournament.entry_fee, room.id)
            self.db.delete(player)
            self.db.delete(room)

        self.db.commit()

    def _cleanup_finished_rooms(self) -> None:
        cutoff = datetime.utcnow() - timedelta(hours=1)
        rooms = (
            self.db.query(TournamentRoom)
            .filter(
                TournamentRoom.status == "finished",
                TournamentRoom.ended_at.isnot(None),
                TournamentRoom.ended_at <= cutoff,
            )
            .all()
        )
        for room in rooms:
            self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).delete()
            self.db.delete(room)
        if rooms:
            self.db.commit()

    def should_start(self, room: TournamentRoom, tournament: TournamentDefinition) -> bool:
        if room.status != "waiting":
            return room.status in {"starting", "active"}

        count = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).count()
        if count >= room.max_players:
            return True

        wait_seconds = tournament.waiting_seconds
        deadline = room.created_at + timedelta(seconds=wait_seconds)
        return count >= 2 and datetime.utcnow() >= deadline

    def try_instant_finalize(self, room_id: str) -> list[dict] | None:
        room = (
            self.db.query(TournamentRoom)
            .filter(TournamentRoom.id == room_id)
            .with_for_update()
            .first()
        )
        if not room or room.status in {"finished", "locked"}:
            return None

        if room.tournament_id == "duel_1v1":
            return self._try_duel_instant_win(room)

        paid_slots = get_paid_rank_count(room.tournament_id)
        if paid_slots <= 0:
            return None

        players = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room_id).all()
        submitted = [p for p in players if p.submitted_at is not None]
        required = min(paid_slots, len(players))
        if len(submitted) < required:
            return None

        room.status = "locked"
        self.db.commit()
        return self.finalize_room(room_id)

    def _try_duel_instant_win(self, room: TournamentRoom) -> list[dict] | None:
        """1v1 duel: first valid completion instantly wins and ends the match for both players."""
        if room.status in {"finished", "locked"}:
            return None

        players = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).all()
        submitted = [p for p in players if p.submitted_at is not None]
        if not submitted:
            return None

        room.status = "locked"
        self.db.commit()
        schedule_room_updated(room.id, serialize_room(self.db, room))
        return self.finalize_room(room.id)

    def try_auto_finalize(self, room_id: str) -> bool:
        return self.try_instant_finalize(room_id) is not None

    def finalize_room(self, room_id: str) -> list[dict]:
        room = self.db.query(TournamentRoom).filter(TournamentRoom.id == room_id).first()
        if not room:
            raise ValueError("Room not found")
        if room.status == "finished":
            return []

        players = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room_id).all()
        users = {
            u.id: u
            for u in self.db.query(User).filter(User.id.in_([p.user_id for p in players])).all()
        }

        ranked = rank_players(
            [
                RankedPlayer(
                    user_id=p.user_id,
                    display_name=users.get(p.user_id).display_name if users.get(p.user_id) else str(p.user_id),
                    score=p.score,
                    elapsed_seconds=p.elapsed_seconds,
                    moves=p.moves,
                    submitted_at=p.submitted_at,
                )
                for p in players
            ]
        )

        results = []
        for player, rank in ranked:
            prize = get_prize(room.tournament_id, rank)
            db_player = next(p for p in players if p.user_id == player.user_id)
            db_player.rank = rank
            db_player.prize = prize
            if not db_player.finished_at:
                db_player.finished_at = datetime.utcnow()

            if prize > 0:
                self.wallet.credit_prize(player.user_id, prize, room_id, rank)

            self.db.add(
                TournamentResult(
                    room_id=room_id,
                    tournament_id=room.tournament_id,
                    user_id=player.user_id,
                    rank=rank,
                    score=player.score,
                    prize=prize,
                )
            )

            entry = self.db.query(LeaderboardEntry).filter(LeaderboardEntry.user_id == player.user_id).first()
            if not entry:
                entry = LeaderboardEntry(user_id=player.user_id)
                self.db.add(entry)
            entry.tournaments_played += 1
            entry.total_prize += prize
            if rank == 1:
                entry.total_wins += 1
            if rank < entry.best_rank:
                entry.best_rank = rank

            results.append(
                {
                    "user_id": player.user_id,
                    "user_uuid": users.get(player.user_id).user_uuid if users.get(player.user_id) else None,
                    "rank": rank,
                    "score": player.score,
                    "prize": prize,
                }
            )

        room.status = "finished"
        room.ended_at = datetime.utcnow()
        self.db.commit()
        schedule_match_finished_broadcast(room_id, results)
        return results
