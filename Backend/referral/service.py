"""WXO 6-Level Tournament Rewards Program.

When a member pays a tournament entry fee, each upline in their referral chain
earns WXO Points: L1 = 5%, L2..L6 = 1% each (10% total distribution).
"""

from __future__ import annotations

from datetime import date, datetime, timedelta

from sqlalchemy import func
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from database.models import ReferralEarning, User
from wallet.service import WalletService

# Level -> percent of the eligible entry fee.
LEVEL_PERCENTS: dict[int, float] = {1: 5.0, 2: 1.0, 3: 1.0, 4: 1.0, 5: 1.0, 6: 1.0}
MAX_LEVEL = 6
TOTAL_PERCENT = sum(LEVEL_PERCENTS.values())

_CODE_PREFIX = "WXO"


def build_referral_code(user_id: int) -> str:
    return f"{_CODE_PREFIX}{user_id:06X}".upper()


class ReferralService:
    def __init__(self, db: Session):
        self.db = db
        self.wallet = WalletService(db)

    # ------------------------------------------------------------------ codes

    def ensure_code(self, user: User) -> str:
        """Assign a stable referral code to a user that does not have one."""
        if user.referral_code:
            return user.referral_code
        user.referral_code = build_referral_code(user.id)
        self.db.flush()
        return user.referral_code

    def find_by_code(self, code: str) -> User | None:
        if not code:
            return None
        return (
            self.db.query(User)
            .filter(func.upper(User.referral_code) == code.strip().upper())
            .first()
        )

    def attach_sponsor(self, user: User, referral_code: str | None) -> bool:
        """Link a freshly registered user to their sponsor. Idempotent."""
        if not referral_code or user.referred_by:
            return False
        sponsor = self.find_by_code(referral_code)
        if not sponsor or sponsor.id == user.id:
            return False
        # Guard against a cycle: sponsor must not already sit below this user.
        if user.id in {u.id for u in self.get_upline(sponsor)}:
            return False
        user.referred_by = sponsor.id
        self.db.flush()
        return True

    # ------------------------------------------------------------------ chain

    def get_upline(self, user: User, max_level: int = MAX_LEVEL) -> list[User]:
        """Return uplines ordered nearest-first (index 0 == level 1)."""
        chain: list[User] = []
        seen: set[int] = {user.id}
        current = user
        for _ in range(max_level):
            if not current.referred_by:
                break
            sponsor = self.db.query(User).filter(User.id == current.referred_by).first()
            if not sponsor or sponsor.id in seen:
                break
            chain.append(sponsor)
            seen.add(sponsor.id)
            current = sponsor
        return chain

    def team_counts(self, user: User) -> dict[int, int]:
        """Members per level below `user`, level 1..6."""
        counts: dict[int, int] = {}
        current_ids = [user.id]
        for level in range(1, MAX_LEVEL + 1):
            if not current_ids:
                counts[level] = 0
                continue
            rows = (
                self.db.query(User.id)
                .filter(User.referred_by.in_(current_ids))
                .all()
            )
            current_ids = [r[0] for r in rows]
            counts[level] = len(current_ids)
        return counts

    def direct_members(self, user: User, limit: int = 200) -> list[dict]:
        """Level-1 (direct) referrals of `user`, newest first.

        Each entry also carries how many people that member has personally
        sponsored, so the UI can show who is building a team of their own.
        """
        rows = (
            self.db.query(User)
            .filter(User.referred_by == user.id)
            .order_by(User.created_at.desc())
            .limit(limit)
            .all()
        )
        members: list[dict] = []
        for u in rows:
            sub_directs = (
                self.db.query(func.count(User.id))
                .filter(User.referred_by == u.id)
                .scalar()
            )
            members.append(
                {
                    "id": u.id,
                    "name": u.display_name or u.username or "Player",
                    "referral_code": u.referral_code,
                    "is_guest": bool(u.is_guest),
                    "joined_at": u.created_at,
                    "directs": int(sub_directs or 0),
                }
            )
        return members

    # ---------------------------------------------------------------- payouts

    @staticmethod
    def points_for(entry_fee: int, level: int) -> int:
        percent = LEVEL_PERCENTS.get(level, 0.0)
        return int(entry_fee * percent / 100)

    def award_entry_fee(
        self,
        payer: User,
        entry_fee: int,
        room_id: str,
        tournament_id: str | None = None,
    ) -> list[ReferralEarning]:
        """Credit WXO Points to up to 6 uplines of `payer`. Safe to re-run."""
        if entry_fee <= 0:
            return []

        awarded: list[ReferralEarning] = []
        for index, sponsor in enumerate(self.get_upline(payer), start=1):
            points = self.points_for(entry_fee, index)
            if points <= 0:
                continue

            duplicate = (
                self.db.query(ReferralEarning.id)
                .filter(
                    ReferralEarning.user_id == sponsor.id,
                    ReferralEarning.from_user_id == payer.id,
                    ReferralEarning.room_id == room_id,
                    ReferralEarning.level == index,
                )
                .first()
            )
            if duplicate:
                continue

            earning = ReferralEarning(
                user_id=sponsor.id,
                from_user_id=payer.id,
                level=index,
                percent=LEVEL_PERCENTS[index],
                entry_fee=entry_fee,
                points=points,
                room_id=room_id,
                tournament_id=tournament_id,
                status="credited",
            )
            self.db.add(earning)
            try:
                self.db.flush()
            except IntegrityError:
                self.db.rollback()
                continue

            self.wallet.ensure_wallet(sponsor.id)
            self.wallet.credit_referral(
                sponsor.id,
                points,
                room_id=room_id,
                level=index,
                from_user_id=payer.id,
            )
            awarded.append(earning)

        self.db.commit()
        return awarded

    # ----------------------------------------------------------------- income

    def _sum_between(self, user_id: int, start: datetime | None) -> int:
        query = self.db.query(func.coalesce(func.sum(ReferralEarning.points), 0)).filter(
            ReferralEarning.user_id == user_id
        )
        if start is not None:
            query = query.filter(ReferralEarning.created_at >= start)
        return int(query.scalar() or 0)

    def income_summary(self, user_id: int) -> dict:
        today_start = datetime.combine(date.today(), datetime.min.time())
        week_start = today_start - timedelta(days=6)
        month_start = today_start - timedelta(days=29)

        by_level_rows = (
            self.db.query(
                ReferralEarning.level,
                func.coalesce(func.sum(ReferralEarning.points), 0),
                func.count(ReferralEarning.id),
            )
            .filter(ReferralEarning.user_id == user_id)
            .group_by(ReferralEarning.level)
            .all()
        )
        by_level = {int(level): {"points": int(points), "entries": int(count)} for level, points, count in by_level_rows}

        return {
            "today": self._sum_between(user_id, today_start),
            "week": self._sum_between(user_id, week_start),
            "month": self._sum_between(user_id, month_start),
            "total": self._sum_between(user_id, None),
            "by_level": [
                {
                    "level": level,
                    "percent": LEVEL_PERCENTS[level],
                    "points": by_level.get(level, {}).get("points", 0),
                    "entries": by_level.get(level, {}).get("entries", 0),
                }
                for level in range(1, MAX_LEVEL + 1)
            ],
        }

    def recent_earnings(self, user_id: int, limit: int = 50) -> list[dict]:
        rows = (
            self.db.query(ReferralEarning, User.display_name)
            .join(User, User.id == ReferralEarning.from_user_id)
            .filter(ReferralEarning.user_id == user_id)
            .order_by(ReferralEarning.created_at.desc())
            .limit(limit)
            .all()
        )
        return [
            {
                "id": e.id,
                "level": e.level,
                "percent": float(e.percent),
                "entry_fee": e.entry_fee,
                "points": e.points,
                "from_name": name or "Player",
                "room_id": e.room_id,
                "tournament_id": e.tournament_id,
                "created_at": e.created_at,
            }
            for e, name in rows
        ]
