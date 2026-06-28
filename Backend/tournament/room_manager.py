import uuid
from datetime import datetime, timedelta

from sqlalchemy.orm import Session

from database.models import LeaderboardEntry, RoomPlayer, TournamentResult, TournamentRoom, User
from tournament.catalog import TournamentDefinition, get_tournament
from tournament.level_selector import generate_room_seed, pick_level_index
from tournament.prize_table import get_prize
from tournament.ranking import RankedPlayer, rank_players
from wallet.service import WalletService


class RoomManager:
    def __init__(self, db: Session):
        self.db = db
        self.wallet = WalletService(db)

    def find_or_create_room(self, tournament_id: str, user_id: int) -> TournamentRoom:
        tournament = get_tournament(tournament_id)
        if not tournament:
            raise ValueError("Tournament not found")

        room = (
            self.db.query(TournamentRoom)
            .filter(
                TournamentRoom.tournament_id == tournament_id,
                TournamentRoom.status == "waiting",
            )
            .order_by(TournamentRoom.created_at.asc())
            .first()
        )

        if room:
            count = (
                self.db.query(RoomPlayer)
                .filter(RoomPlayer.room_id == room.id)
                .count()
            )
            if count < room.max_players:
                return room

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

    def join_room(self, room: TournamentRoom, user_id: int, tournament: TournamentDefinition) -> RoomPlayer:
        existing = (
            self.db.query(RoomPlayer)
            .filter(RoomPlayer.room_id == room.id, RoomPlayer.user_id == user_id)
            .first()
        )
        if existing:
            return existing

        count = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).count()
        if count >= room.max_players:
            raise ValueError("Room is full")

        self.wallet.deduct_entry_fee(user_id, tournament.entry_fee, room.id)

        player = RoomPlayer(room_id=room.id, user_id=user_id)
        self.db.add(player)
        self.db.commit()
        self.db.refresh(player)

        count = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).count()
        if count >= room.max_players:
            self._activate_room(room)
        elif self.should_start(room, tournament):
            self._activate_room(room)

        return player

    def _activate_room(self, room: TournamentRoom) -> None:
        if room.status != "waiting":
            return
        room.status = "active"
        room.started_at = datetime.utcnow()
        self.db.commit()

    def should_start(self, room: TournamentRoom, tournament: TournamentDefinition) -> bool:
        if room.status != "waiting":
            return room.status in {"starting", "active"}

        count = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).count()
        if count >= room.max_players:
            return True

        tournament_def = get_tournament(room.tournament_id)
        wait_seconds = tournament_def.waiting_seconds if tournament_def else tournament.waiting_seconds
        deadline = room.created_at + timedelta(seconds=wait_seconds)
        return count >= 2 and datetime.utcnow() >= deadline

    def try_auto_finalize(self, room_id: str) -> bool:
        room = self.db.query(TournamentRoom).filter(TournamentRoom.id == room_id).first()
        if not room or room.status == "finished":
            return False

        players = self.db.query(RoomPlayer).filter(RoomPlayer.room_id == room_id).all()
        if not players:
            return False

        submitted = [p for p in players if p.submitted_at is not None]
        if len(submitted) >= len(players) or (
            room.started_at
            and datetime.utcnow() >= room.started_at + timedelta(hours=2)
            and len(submitted) >= max(1, len(players) // 2)
        ):
            self.finalize_room(room_id)
            return True
        return False

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
        return results
