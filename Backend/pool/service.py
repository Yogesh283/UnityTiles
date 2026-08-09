"""IQFX Pro Pool Play service.

A pool is a time-boxed leaderboard tournament:
  * players pay a fixed entry fee to join,
  * each plays the same level once and submits a score,
  * when the pool fills (or an admin closes it) the top-N scores share the prize pool
    (= 70% of the actual collection) using the pool's distribution table.

Money movement reuses the idempotent WalletService, so retries never double-charge or
double-pay.
"""

from __future__ import annotations

import logging
import random
from datetime import datetime

from sqlalchemy.orm import Session

from database.models import Pool, PoolEntry, User
from pool import rules
from wallet.service import WalletService

logger = logging.getLogger("matchiq.pool")


class PoolError(ValueError):
    """Domain error surfaced to the API as a 400."""


class PoolService:
    def __init__(self, db: Session):
        self.db = db
        self.wallet = WalletService(db)

    # -- creation -----------------------------------------------------------------
    def create_pool(
        self,
        *,
        entry_fee: int,
        max_players: int,
        name: str | None = None,
        icon: str = "🏆",
        winners_count: int | None = None,
        distribution: list[int] | None = None,
        prize_share: float = rules.PRIZE_SHARE,
        level_index: int = 0,
        created_by: str | None = None,
    ) -> Pool:
        if entry_fee < 0:
            raise PoolError("Entry fee must be >= 0")
        if max_players < 2:
            raise PoolError("A pool needs at least 2 players")

        winners = winners_count or rules.winners_for(max_players)
        winners = max(1, min(winners, max_players))
        dist = distribution or rules.distribution_percents(winners)
        advertised = rules.prize_pool(max_players, entry_fee, prize_share)

        pool = Pool(
            name=name or f"IQFX Pro · ₹{entry_fee} · {max_players} Players",
            icon=icon,
            entry_fee=entry_fee,
            max_players=max_players,
            winners_count=winners,
            prize_share=prize_share,
            prize_pool=advertised,
            distribution=_dumps(dist),
            level_index=level_index,
            level_seed=random.randint(1, 2_000_000_000),
            status="open",
            created_by=created_by,
        )
        self.db.add(pool)
        self.db.commit()
        self.db.refresh(pool)
        return pool

    # -- reads --------------------------------------------------------------------
    def list_open_pools(self) -> list[dict]:
        pools = (
            self.db.query(Pool)
            .filter(Pool.status.in_(("open", "running")))
            .order_by(Pool.entry_fee.asc(), Pool.created_at.asc())
            .all()
        )
        return [self.serialize_pool(p) for p in pools]

    def get_pool(self, pool_id: int) -> Pool | None:
        return self.db.query(Pool).filter(Pool.id == pool_id).first()

    def joined_count(self, pool_id: int) -> int:
        return self.db.query(PoolEntry).filter(PoolEntry.pool_id == pool_id).count()

    def serialize_pool(self, pool: Pool, user_id: int | None = None) -> dict:
        joined = self.joined_count(pool.id)
        distribution = rules.parse_distribution(pool.distribution, pool.winners_count)
        # Advertised prize pool is for a full pool; also expose the current (live) prize pool.
        live_pool = rules.prize_pool(max(joined, 0), pool.entry_fee, float(pool.prize_share))
        data = {
            "id": pool.id,
            "name": pool.name,
            "icon": pool.icon,
            "entry_fee": pool.entry_fee,
            "max_players": pool.max_players,
            "players_joined": joined,
            "winners_count": pool.winners_count,
            "prize_share": float(pool.prize_share),
            "prize_pool": pool.prize_pool,
            "live_prize_pool": live_pool,
            "distribution": distribution,
            "prize_breakdown": rules.split_prize(pool.prize_pool, distribution),
            "status": pool.status,
            "level_index": pool.level_index,
            "level_seed": pool.level_seed,
            "created_at": pool.created_at.isoformat() if pool.created_at else None,
            "finished_at": pool.finished_at.isoformat() if pool.finished_at else None,
        }
        if user_id is not None:
            entry = self._entry(pool.id, user_id)
            data["my_entry"] = self.serialize_entry(entry) if entry else None
        return data

    def serialize_entry(self, entry: PoolEntry) -> dict:
        return {
            "pool_id": entry.pool_id,
            "user_id": entry.user_id,
            "score": entry.score,
            "rank": entry.rank,
            "prize": entry.prize,
            "status": entry.status,
            "submitted_at": entry.submitted_at.isoformat() if entry.submitted_at else None,
        }

    def _entry(self, pool_id: int, user_id: int) -> PoolEntry | None:
        return (
            self.db.query(PoolEntry)
            .filter(PoolEntry.pool_id == pool_id, PoolEntry.user_id == user_id)
            .first()
        )

    # -- join ---------------------------------------------------------------------
    def join_pool(self, user: User, pool_id: int) -> dict:
        # Lock the pool row so concurrent joins can't oversubscribe it.
        pool = self.db.query(Pool).filter(Pool.id == pool_id).with_for_update().first()
        if not pool:
            raise PoolError("Pool not found")
        if pool.status not in ("open", "running"):
            raise PoolError("Pool is not open for entries")

        existing = self._entry(pool_id, user.id)
        if existing:
            # Idempotent: already joined, just return current state.
            return self.serialize_pool(pool, user_id=user.id)

        joined = self.joined_count(pool_id)
        if joined >= pool.max_players:
            raise PoolError("Pool is full")

        if pool.entry_fee > 0 and self.wallet.get_balance(user.id) < pool.entry_fee:
            raise PoolError("Insufficient balance")

        if pool.entry_fee > 0:
            self.wallet.deduct_pool_entry(user.id, pool.entry_fee, pool.id)
            self._award_referral(user, pool)

        entry = PoolEntry(
            pool_id=pool.id,
            user_id=user.id,
            entry_fee=pool.entry_fee,
            status="joined",
        )
        self.db.add(entry)

        if joined + 1 >= pool.max_players and pool.status == "open":
            pool.status = "running"

        self.db.commit()
        self.db.refresh(pool)
        return self.serialize_pool(pool, user_id=user.id)

    def _award_referral(self, user: User, pool: Pool) -> None:
        """WXO 6-level referral payout on the entry fee. Never blocks a join."""
        try:
            from referral.service import ReferralService

            ReferralService(self.db).award_entry_fee(
                user,
                pool.entry_fee,
                f"pool:{pool.id}",
                tournament_id=f"pool:{pool.id}",
            )
        except Exception:  # pragma: no cover - defensive
            self.db.rollback()
            logger.exception("Referral payout failed for pool %s user %s", pool.id, user.id)

    # -- score submission ---------------------------------------------------------
    def submit_score(
        self,
        user: User,
        pool_id: int,
        score: int,
        *,
        moves: int = 0,
        elapsed_seconds: int = 0,
    ) -> dict:
        entry = self._entry(pool_id, user.id)
        if not entry:
            raise PoolError("You have not joined this pool")

        pool = self.get_pool(pool_id)
        if pool and pool.status == "finished":
            # Pool already settled; return the final state.
            return self.serialize_pool(pool, user_id=user.id)

        # Keep the best score if a higher one is resubmitted.
        if entry.status != "played" or score > entry.score:
            entry.score = max(score, entry.score)
            entry.moves = moves
            entry.elapsed_seconds = elapsed_seconds
        entry.status = "played"
        entry.submitted_at = datetime.utcnow()
        self.db.commit()

        self._maybe_finalize(pool_id)

        pool = self.get_pool(pool_id)
        return self.serialize_pool(pool, user_id=user.id)

    def _maybe_finalize(self, pool_id: int) -> None:
        pool = self.get_pool(pool_id)
        if not pool or pool.status == "finished":
            return

        joined = self.joined_count(pool_id)
        played = (
            self.db.query(PoolEntry)
            .filter(PoolEntry.pool_id == pool_id, PoolEntry.status == "played")
            .count()
        )
        full = joined >= pool.max_players
        deadline_passed = bool(pool.ends_at and datetime.utcnow() >= pool.ends_at)

        # Settle when everyone who paid has played a full pool, or the deadline passed.
        if (full and played >= joined and joined > 0) or (deadline_passed and played > 0):
            self.finalize(pool_id)

    # -- finalize / payout --------------------------------------------------------
    def finalize(self, pool_id: int) -> dict:
        pool = self.db.query(Pool).filter(Pool.id == pool_id).with_for_update().first()
        if not pool:
            raise PoolError("Pool not found")
        if pool.status == "finished":
            return self.serialize_pool(pool)

        entries = self.db.query(PoolEntry).filter(PoolEntry.pool_id == pool_id).all()
        collection = sum(e.entry_fee for e in entries)
        total_prize = int(collection * float(pool.prize_share))

        played = [e for e in entries if e.status == "played" or e.submitted_at is not None]
        # Rank: highest score wins; ties broken by faster time, then fewer moves, then earlier submit.
        played.sort(
            key=lambda e: (
                -e.score,
                e.elapsed_seconds if e.elapsed_seconds else 1 << 30,
                e.moves if e.moves else 1 << 30,
                e.submitted_at or datetime.max,
            )
        )

        distribution = rules.parse_distribution(pool.distribution, pool.winners_count)
        prize_amounts = rules.split_prize(total_prize, distribution)

        credits: list[tuple[int, int, int]] = []
        for index, entry in enumerate(played):
            rank = index + 1
            prize = prize_amounts[index] if index < len(prize_amounts) else 0
            entry.rank = rank
            entry.prize = prize
            entry.status = "won" if prize > 0 else "lost"
            if prize > 0:
                credits.append((entry.user_id, prize, rank))

        # Entries that never played get no prize.
        for entry in entries:
            if entry.rank is None:
                entry.status = "lost"

        pool.prize_pool = total_prize
        pool.status = "finished"
        pool.finished_at = datetime.utcnow()
        self.db.commit()

        for user_id, prize, rank in credits:
            try:
                self.wallet.credit_pool_prize(user_id, prize, pool.id, rank)
            except Exception:  # pragma: no cover - defensive
                logger.exception("Prize credit failed pool=%s user=%s", pool.id, user_id)

        logger.info(
            "Pool %s finalized: players=%s collection=%s prize=%s winners=%s",
            pool.id, len(entries), collection, total_prize, len(credits),
        )
        self.db.refresh(pool)
        return self.serialize_pool(pool)

    def close_pool(self, pool_id: int) -> dict:
        """Admin action: settle a pool early with whoever has played so far."""
        return self.finalize(pool_id)

    # -- history ------------------------------------------------------------------
    def user_history(self, user_id: int, limit: int = 50) -> list[dict]:
        rows = (
            self.db.query(PoolEntry, Pool)
            .join(Pool, Pool.id == PoolEntry.pool_id)
            .filter(PoolEntry.user_id == user_id)
            .order_by(PoolEntry.joined_at.desc())
            .limit(limit)
            .all()
        )
        history = []
        for entry, pool in rows:
            history.append(
                {
                    "pool_id": pool.id,
                    "name": pool.name,
                    "icon": pool.icon,
                    "entry_fee": pool.entry_fee,
                    "status": pool.status,
                    "my_status": entry.status,
                    "score": entry.score,
                    "rank": entry.rank,
                    "prize": entry.prize,
                    "joined_at": entry.joined_at.isoformat() if entry.joined_at else None,
                }
            )
        return history


def _dumps(distribution: list[int]) -> str:
    import json

    return json.dumps(distribution)
