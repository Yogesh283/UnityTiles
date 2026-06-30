#!/usr/bin/env python3
"""Verify finalize_room completes atomically (no mid-loop wallet commit)."""

from __future__ import annotations

import os
import sys
import uuid
from datetime import datetime, timedelta
from unittest.mock import patch

BACKEND = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, BACKEND)
os.chdir(BACKEND)

from sqlalchemy import create_engine, event
from sqlalchemy.orm import sessionmaker

from database.models import Base, LeaderboardEntry, RoomPlayer, Tournament, TournamentResult, TournamentRoom, User, Wallet
from tournament.catalog import TOURNAMENT_CATALOG
from tournament.room_manager import RoomManager


def _enable_sqlite_fks(dbapi_conn, _):
    dbapi_conn.execute("PRAGMA foreign_keys=ON")


def main() -> int:
    engine = create_engine("sqlite:///:memory:")
    event.listen(engine, "connect", _enable_sqlite_fks)
    Base.metadata.create_all(engine)
    Session = sessionmaker(bind=engine)
    db = Session()

    duel = next(t for t in TOURNAMENT_CATALOG if t.id == "duel_1v1")
    db.add(
        Tournament(
            id=duel.id,
            display_name=duel.display_name,
            icon=duel.icon,
            max_players=duel.max_players,
            entry_fee=duel.entry_fee,
            prize_pool=duel.prize_pool,
            platform_fee=duel.platform_fee,
            reward_info=duel.reward_info,
            waiting_seconds=duel.waiting_seconds,
            status_label=duel.status_label,
        )
    )

    u1 = User(user_uuid=str(uuid.uuid4()), display_name="P1", is_guest=True, guest_id="g1")
    u2 = User(user_uuid=str(uuid.uuid4()), display_name="P2", is_guest=True, guest_id="g2")
    db.add_all([u1, u2])
    db.flush()
    db.add_all([Wallet(user_id=u1.id, balance=500), Wallet(user_id=u2.id, balance=500)])

    room = TournamentRoom(
        id="duel_1v1_testroom01",
        tournament_id="duel_1v1",
        level_index=61,
        level_seed=-2137062531,
        status="locked",
        max_players=2,
        started_at=datetime.utcnow() - timedelta(seconds=30),
    )
    db.add(room)
    db.flush()
    now = datetime.utcnow()
    db.add(
        RoomPlayer(
            room_id=room.id,
            user_id=u1.id,
            score=1200,
            moves=42,
            elapsed_seconds=30,
            submitted_at=now,
            finished_at=now,
            rank=1,
            prize=160,
        )
    )
    db.add(RoomPlayer(room_id=room.id, user_id=u2.id, score=0, moves=0, elapsed_seconds=0))
    db.commit()

    credit_calls: list[tuple[int, int]] = []
    original_credit = RoomManager(db).wallet.credit_prize

    def track_credit(user_id: int, amount: int, room_id: str, rank: int):
        credit_calls.append((user_id, amount))
        return original_credit(user_id, amount, room_id, rank)

    with patch("tournament.room_manager.schedule_match_finished_broadcast"):
        manager = RoomManager(db)
        with patch.object(manager.wallet, "credit_prize", side_effect=track_credit):
            results = manager.finalize_room(room.id)

    db.refresh(room)
    p2 = db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id, RoomPlayer.user_id == u2.id).first()
    results_count = db.query(TournamentResult).filter(TournamentResult.room_id == room.id).count()
    leaderboard_count = db.query(LeaderboardEntry).count()

    checks = [
        ("room finished", room.status == "finished"),
        ("p2 ranked", p2 is not None and p2.rank == 2),
        ("two results", results_count == 2),
        ("leaderboard rows", leaderboard_count == 2),
        ("prize credited once", credit_calls == [(u1.id, 160)]),
        ("results payload", len(results) == 2),
    ]
    failed = [name for name, ok in checks if not ok]
    for name, ok in checks:
        print(f"{'PASS' if ok else 'FAIL'}: {name}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
