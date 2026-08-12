"""WXO 6-Level Tournament Rewards Program.

When a member pays a tournament entry fee, each upline in their referral chain
earns WXO Points: L1 = 5%, L2..L6 = 1% each (10% total distribution).
"""

from __future__ import annotations

from datetime import date, datetime, timedelta

from sqlalchemy import func
from sqlalchemy.exc import IntegrityError
from sqlalchemy.orm import Session

from database.models import ReferralEarning, User, WalletTransaction
from wallet.service import WalletService

# Level -> percent of the eligible entry fee.
LEVEL_PERCENTS: dict[int, float] = {1: 5.0, 2: 1.0, 3: 1.0, 4: 1.0, 5: 1.0, 6: 1.0}
MAX_LEVEL = 6
TOTAL_PERCENT = sum(LEVEL_PERCENTS.values())

_CODE_PREFIX = "WXO"

_PRIZE_TYPE_LABELS = {
    "tournament_prize": "Tournament Win",
    "pool_prize": "Pool Prize",
    "tournament_level_reward": "Level Reward",
    "level_complete_reward": "Level Complete",
    "admin_adjust": "Bonus",
}


def _level_type_label(level: int) -> str:
    if level == 1:
        return "Level 1 Income"
    return f"Level {level} Income"


def _day_bounds(day: date) -> tuple[datetime, datetime]:
    start = datetime.combine(day, datetime.min.time())
    return start, start + timedelta(days=1)


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

    def find_sponsor(self, referral_code: str | None) -> User | None:
        """Accept referral code, numeric User ID, or a register URL with ?ref=."""
        raw = self._normalize_ref_input(referral_code)
        if not raw:
            return None
        found = self.find_by_code(raw)
        if found:
            return found
        if raw.isdigit():
            return self.db.query(User).filter(User.id == int(raw)).first()
        return None

    @staticmethod
    def _normalize_ref_input(value: str | None) -> str:
        s = (value or "").strip()
        if not s:
            return ""
        lower = s.lower()
        if "ref=" in lower:
            try:
                from urllib.parse import parse_qs, urlparse

                q = parse_qs(urlparse(s).query)
                if q.get("ref"):
                    s = str(q["ref"][0]).strip()
            except Exception:
                pass
        return s

    def attach_sponsor(self, user: User, referral_code: str | None) -> bool:
        """Link a freshly registered user to their sponsor. Idempotent."""
        if not referral_code or user.referred_by:
            return False
        sponsor = self.find_sponsor(referral_code)
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

    def referral_link(self, code: str) -> str:
        from config import get_settings

        base = (get_settings().public_site_url or "https://rmsurveyai.com").rstrip("/")
        return f"{base}/register.html?ref={code}"

    def team_snapshot(self, user: User) -> dict:
        counts = self.team_counts(user)
        direct = int(counts.get(1, 0))
        sat = sum(int(counts.get(lvl, 0)) for lvl in range(2, MAX_LEVEL + 1))
        return {
            "direct_count": direct,
            "sat_count": sat,
            "team_size": direct + sat,
            "counts": counts,
        }

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
                    "joined_at": u.created_at.isoformat() if u.created_at else None,
                    "directs": int(sub_directs or 0),
                }
            )
        return members

    def members_by_level(self, user: User, per_level: int = 80) -> list[dict]:
        """L1–L6 downline lists for the team structure UI."""
        levels: list[dict] = []
        current_ids = [user.id]
        for level in range(1, MAX_LEVEL + 1):
            if not current_ids:
                levels.append(
                    {
                        "level": level,
                        "percent": LEVEL_PERCENTS[level],
                        "label": "Direct" if level == 1 else f"Sat L{level}",
                        "count": 0,
                        "members": [],
                    }
                )
                continue
            rows = (
                self.db.query(User)
                .filter(User.referred_by.in_(current_ids))
                .order_by(User.created_at.desc())
                .all()
            )
            current_ids = [u.id for u in rows]
            shown = rows[:per_level]
            levels.append(
                {
                    "level": level,
                    "percent": LEVEL_PERCENTS[level],
                    "label": "Direct" if level == 1 else f"Sat L{level}",
                    "count": len(rows),
                    "members": [
                        {
                            "id": u.id,
                            "name": u.display_name or u.username or "Player",
                            "referral_code": u.referral_code,
                            "is_guest": bool(u.is_guest),
                            "joined_at": u.created_at.isoformat() if u.created_at else None,
                        }
                        for u in shown
                    ],
                }
            )
        return levels

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

        direct_pts = int(by_level.get(1, {}).get("points", 0))
        sat_pts = sum(int(by_level.get(lvl, {}).get("points", 0)) for lvl in range(2, MAX_LEVEL + 1))

        return {
            "today": self._sum_between(user_id, today_start),
            "week": self._sum_between(user_id, week_start),
            "month": self._sum_between(user_id, month_start),
            "total": self._sum_between(user_id, None),
            "direct": direct_pts,
            "sat": sat_pts,
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

    def recent_earnings(
        self,
        user_id: int,
        limit: int = 50,
        day: date | None = None,
    ) -> list[dict]:
        query = (
            self.db.query(ReferralEarning, User.display_name)
            .join(User, User.id == ReferralEarning.from_user_id)
            .filter(ReferralEarning.user_id == user_id)
        )
        if day is not None:
            start, end = _day_bounds(day)
            query = query.filter(ReferralEarning.created_at >= start, ReferralEarning.created_at < end)
        rows = query.order_by(ReferralEarning.created_at.desc()).limit(limit).all()
        return [
            {
                "id": e.id,
                "level": e.level,
                "percent": float(e.percent),
                "entry_fee": e.entry_fee,
                "points": e.points,
                "from_name": name or "Player",
                "user": name or "Player",
                "type": _level_type_label(e.level),
                "type_key": f"level_{e.level}",
                "room_id": e.room_id,
                "tournament_id": e.tournament_id,
                "created_at": e.created_at,
            }
            for e, name in rows
        ]

    def _wallet_income(self, user_id: int, start: datetime | None, end: datetime | None) -> list[dict]:
        query = self.db.query(WalletTransaction).filter(
            WalletTransaction.user_id == user_id,
            WalletTransaction.amount > 0,
            WalletTransaction.type.in_(tuple(_PRIZE_TYPE_LABELS.keys())),
        )
        if start is not None:
            query = query.filter(WalletTransaction.created_at >= start)
        if end is not None:
            query = query.filter(WalletTransaction.created_at < end)
        rows = query.order_by(WalletTransaction.created_at.desc()).all()
        items: list[dict] = []
        for row in rows:
            items.append(
                {
                    "id": f"w{row.id}",
                    "user": "You",
                    "type": _PRIZE_TYPE_LABELS.get(row.type, row.type),
                    "type_key": row.type,
                    "level": None,
                    "percent": None,
                    "entry_fee": None,
                    "points": int(row.amount or 0),
                    "from_name": "You",
                    "room_id": row.reference_id,
                    "tournament_id": None,
                    "created_at": row.created_at,
                }
            )
        return items

    def _sum_wallet_income(self, user_id: int, start: datetime | None, end: datetime | None = None) -> int:
        query = self.db.query(func.coalesce(func.sum(WalletTransaction.amount), 0)).filter(
            WalletTransaction.user_id == user_id,
            WalletTransaction.amount > 0,
            WalletTransaction.type.in_(tuple(_PRIZE_TYPE_LABELS.keys())),
        )
        if start is not None:
            query = query.filter(WalletTransaction.created_at >= start)
        if end is not None:
            query = query.filter(WalletTransaction.created_at < end)
        return int(query.scalar() or 0)

    def rewards_report(self, user: User, day: date) -> dict:
        """All income for one calendar day, plus always-on today/total totals."""
        today = date.today()
        if day > today:
            day = today

        day_start, day_end = _day_bounds(day)
        today_start, today_end = _day_bounds(today)

        level_items = self.recent_earnings(user.id, limit=200, day=day)
        prize_items = self._wallet_income(user.id, day_start, day_end)
        items = sorted(
            level_items + prize_items,
            key=lambda row: row.get("created_at") or datetime.min,
            reverse=True,
        )

        today_points = self._sum_between(user.id, today_start) + self._sum_wallet_income(
            user.id, today_start, today_end
        )
        total_points = self._sum_between(user.id, None) + self._sum_wallet_income(user.id, None)
        selected_points = sum(int(row.get("points") or 0) for row in items)

        by_level_rows = (
            self.db.query(
                ReferralEarning.level,
                func.coalesce(func.sum(ReferralEarning.points), 0),
                func.count(ReferralEarning.id),
            )
            .filter(
                ReferralEarning.user_id == user.id,
                ReferralEarning.created_at >= day_start,
                ReferralEarning.created_at < day_end,
            )
            .group_by(ReferralEarning.level)
            .all()
        )
        by_level_map = {
            int(level): {"points": int(points), "entries": int(count)}
            for level, points, count in by_level_rows
        }

        return {
            "date": day.isoformat(),
            "is_today": day == today,
            "today": today_points,
            "total": total_points,
            "selected": selected_points,
            "count": len(items),
            "items": items,
            "by_level": [
                {
                    "level": level,
                    "percent": LEVEL_PERCENTS[level],
                    "points": by_level_map.get(level, {}).get("points", 0),
                    "entries": by_level_map.get(level, {}).get("entries", 0),
                }
                for level in range(1, MAX_LEVEL + 1)
            ],
        }
