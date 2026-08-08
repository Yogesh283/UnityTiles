"""Smoke test for the WXO 6-level referral reward payout.

Builds a throwaway 7-user chain, pays one entry fee, and asserts each upline
receives the configured percentage as WXO Points.
"""

import sys
import uuid
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from database.connection import SessionLocal  # noqa: E402
from database.models import ReferralEarning, User, Wallet  # noqa: E402
from referral.service import LEVEL_PERCENTS, ReferralService  # noqa: E402

ENTRY_FEE = 1000
ROOM_ID = f"test-room-{uuid.uuid4().hex[:8]}"

db = SessionLocal()
service = ReferralService(db)
created: list[User] = []

try:
    previous: User | None = None
    for depth in range(7):  # sponsor chain: L6 ... L1 ... payer
        user = User(
            user_uuid=str(uuid.uuid4()),
            guest_id=f"reftest-{uuid.uuid4().hex[:10]}",
            display_name=f"RefTest{depth}",
            is_guest=True,
            referred_by=previous.id if previous else None,
        )
        db.add(user)
        db.flush()
        user.referral_code = f"T{user.id:07X}"[:16]
        db.add(Wallet(user_id=user.id, balance=0))
        db.flush()
        created.append(user)
        previous = user

    db.commit()
    payer = created[-1]

    uplines = service.get_upline(payer)
    print(f"upline depth: {len(uplines)}")

    awarded = service.award_entry_fee(payer, ENTRY_FEE, ROOM_ID, tournament_id="t-test")
    print(f"awarded rows: {len(awarded)}")

    total = 0
    for earning in sorted(awarded, key=lambda e: e.level):
        wallet = db.query(Wallet).filter(Wallet.user_id == earning.user_id).first()
        expected = int(ENTRY_FEE * LEVEL_PERCENTS[earning.level] / 100)
        status = "OK" if earning.points == expected == wallet.balance else "MISMATCH"
        total += earning.points
        print(
            f"L{earning.level} {earning.percent}% -> points={earning.points} "
            f"wallet={wallet.balance} expected={expected} [{status}]"
        )

    print(f"total distributed: {total} ({total / ENTRY_FEE:.0%} of entry fee)")

    replay = service.award_entry_fee(payer, ENTRY_FEE, ROOM_ID, tournament_id="t-test")
    print(f"idempotent replay awarded: {len(replay)} (expected 0)")

    summary = service.income_summary(created[-2].id)
    print(f"L1 sponsor income: today={summary['today']} total={summary['total']}")

finally:
    ids = [u.id for u in created]
    if ids:
        db.query(ReferralEarning).filter(ReferralEarning.user_id.in_(ids)).delete(
            synchronize_session=False
        )
        db.query(Wallet).filter(Wallet.user_id.in_(ids)).delete(synchronize_session=False)
        db.query(User).filter(User.id.in_(ids)).update(
            {User.referred_by: None}, synchronize_session=False
        )
        db.query(User).filter(User.id.in_(ids)).delete(synchronize_session=False)
        db.commit()
        print("cleanup done")
    db.close()
