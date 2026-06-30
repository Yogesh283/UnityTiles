from __future__ import annotations

from datetime import datetime, timedelta

from sqlalchemy.orm import Session

from database.models import LeaderboardEntry, RoomPlayer, TournamentRoom, User
from tournament.catalog import get_tournament

MATCH_START_COUNTDOWN_SECONDS = 3


def waiting_seconds_remaining(room: TournamentRoom) -> int:
    tournament = get_tournament(room.tournament_id)
    wait_seconds = tournament.waiting_seconds if tournament else 30
    deadline = room.created_at + timedelta(seconds=wait_seconds)
    return max(0, int((deadline - datetime.utcnow()).total_seconds()))


def start_countdown_remaining(room: TournamentRoom) -> int:
    if room.status != "starting" or not room.started_at:
        return 0
    deadline = room.started_at + timedelta(seconds=MATCH_START_COUNTDOWN_SECONDS)
    return max(0, int((deadline - datetime.utcnow()).total_seconds()))


def match_start_at_ms(room: TournamentRoom) -> int | None:
    if room.status not in {"starting", "active", "locked", "finished"} or not room.started_at:
        return None
    start = room.started_at + timedelta(seconds=MATCH_START_COUNTDOWN_SECONDS)
    return int(start.timestamp() * 1000)


def serialize_player(db: Session, player: RoomPlayer, tournament_id: str) -> dict:
    user = db.query(User).filter(User.id == player.user_id).first()
    leaderboard = (
        db.query(LeaderboardEntry).filter(LeaderboardEntry.user_id == player.user_id).first()
    )
    return {
        "user_id": player.user_id,
        "user_uuid": user.user_uuid if user else None,
        "display_name": user.display_name if user else f"Player {player.user_id}",
        "avatar_url": user.avatar_url if user else None,
        "current_rank": leaderboard.best_rank if leaderboard else 9999,
        "tournament_id": tournament_id,
        "score": player.score,
        "moves": player.moves,
        "elapsed_seconds": player.elapsed_seconds,
        "rank": player.rank,
        "is_connected": player.is_connected,
        "has_submitted": player.submitted_at is not None,
    }


def search_status(room: TournamentRoom, player_count: int, max_players: int) -> str:
    if room.status == "starting":
        return "match_found"
    if room.status in {"active", "locked"}:
        return "starting"
    if player_count >= max_players:
        return "players_connected"
    if player_count >= 2:
        return "player_joined"
    return "searching"


def serialize_room(db: Session, room: TournamentRoom) -> dict:
    tournament = get_tournament(room.tournament_id)
    players = db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).all()
    waiting_seconds = tournament.waiting_seconds if tournament else 30
    player_count = len(players)
    max_players = room.max_players

    return {
        "room_id": room.id,
        "tournament_id": room.tournament_id,
        "tournament_name": tournament.display_name if tournament else room.tournament_id,
        "level_index": room.level_index,
        "level_seed": room.level_seed,
        "status": room.status,
        "search_status": search_status(room, player_count, max_players),
        "player_count": player_count,
        "max_players": max_players,
        "waiting_seconds": waiting_seconds,
        "waiting_seconds_remaining": waiting_seconds_remaining(room),
        "start_countdown_seconds": start_countdown_remaining(room),
        "match_start_at_ms": match_start_at_ms(room),
        "server_now_ms": int(datetime.utcnow().timestamp() * 1000),
        "players": [serialize_player(db, player, room.tournament_id) for player in players],
    }
