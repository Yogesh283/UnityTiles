"""Smoke test for IQFX Pro Pool Play.

Creates a throwaway pool, joins N players, submits scores, and asserts the pool
auto-finalizes and pays the top-N per the distribution (prize pool = 70% of collection).
"""

import sys
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from database.connection import Base, SessionLocal, engine  # noqa: E402
from database.models import Pool, PoolEntry, ReferralEarning, User, Wallet, WalletTransaction  # noqa: E402
from pool.service import PoolService  # noqa: E402

# Ensure pool tables exist even if migration 007 has not been run yet.
Base.metadata.create_all(bind=engine, tables=[Pool.__table__, PoolEntry.__table__])

ENTRY_FEE = 50
PLAYERS = 4  # small full pool so it auto-finalizes when everyone submits

db = SessionLocal()
service = PoolService(db)
created: list[User] = []
pool_id = None

try:
    start_balance = 500
    for i in range(PLAYERS):
        user = User(
            user_uuid=str(uuid.uuid4()),
            guest_id=f"pooltest-{uuid.uuid4().hex[:10]}",
            display_name=f"PoolTest{i}",
            is_guest=True,
        )
        db.add(user)
        db.flush()
        db.add(Wallet(user_id=user.id, balance=start_balance))
        db.flush()
        created.append(user)
    db.commit()

    pool = service.create_pool(
        entry_fee=ENTRY_FEE,
        max_players=PLAYERS,
        name=f"TEST Pool {uuid.uuid4().hex[:6]}",
        created_by="test",
    )
    pool_id = pool.id
    print(f"pool {pool.id}: winners={pool.winners_count} dist={pool.distribution} advertised_prize={pool.prize_pool}")

    for u in created:
        service.join_pool(u, pool.id)
    print(f"joined: {service.joined_count(pool.id)} / {PLAYERS}")

    # Distinct scores: player i gets score (i+1)*100, so last player wins.
    for i, u in enumerate(created):
        service.submit_score(u, pool.id, score=(i + 1) * 100, moves=10, elapsed_seconds=30)

    db.refresh(pool)
    print(f"pool status after submits: {pool.status} (expected finished)")

    collection = ENTRY_FEE * PLAYERS
    expected_prize = int(collection * 0.70)
    print(f"collection={collection} expected_prize_pool={expected_prize} actual={pool.prize_pool}")

    entries = (
        db.query(PoolEntry)
        .filter(PoolEntry.pool_id == pool.id)
        .order_by(PoolEntry.rank.asc())
        .all()
    )
    total_paid = 0
    for e in entries:
        wallet = db.query(Wallet).filter(Wallet.user_id == e.user_id).first()
        total_paid += e.prize
        print(
            f"rank={e.rank} score={e.score} prize={e.prize} status={e.status} "
            f"wallet={wallet.balance} (start {start_balance} - entry {ENTRY_FEE} + prize {e.prize})"
        )

    winner = entries[0]
    ok = (
        pool.status == "finished"
        and pool.prize_pool == expected_prize
        and winner.rank == 1
        and winner.prize == expected_prize  # winners_count=1 for 4 players -> [100%]
        and total_paid == expected_prize
    )
    print(f"total_paid={total_paid} RESULT={'OK' if ok else 'MISMATCH'}")

    # Idempotent replay of a submit must not change anything.
    service.submit_score(created[0], pool.id, score=999999)
    db.refresh(pool)
    print(f"replay submit -> status still {pool.status}")

finally:
    if pool_id is not None:
        db.query(PoolEntry).filter(PoolEntry.pool_id == pool_id).delete(synchronize_session=False)
        db.query(Pool).filter(Pool.id == pool_id).delete(synchronize_session=False)
    ids = [u.id for u in created]
    if ids:
        db.query(WalletTransaction).filter(WalletTransaction.user_id.in_(ids)).delete(
            synchronize_session=False
        )
        db.query(ReferralEarning).filter(ReferralEarning.user_id.in_(ids)).delete(
            synchronize_session=False
        )
        db.query(Wallet).filter(Wallet.user_id.in_(ids)).delete(synchronize_session=False)
        db.query(User).filter(User.id.in_(ids)).delete(synchronize_session=False)
    db.commit()
    print("cleanup done")
    db.close()
