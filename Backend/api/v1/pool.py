"""IQFX Pro Pool Play API.

Players list open pools, pay a fixed entry fee to join, submit their score, and the
top-N share the prize pool (= 70% of collection). Pool creation is intended for admins
(the Laravel panel writes pool rows directly), but a create endpoint is exposed for
tooling/tests.
"""

import logging

from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from core.audit import write_audit_log
from database.connection import get_db
from database.models import User
from models.schemas import CreatePoolRequest, JoinPoolRequest, SubmitPoolScoreRequest
from pool import rules
from pool.service import PoolError, PoolService
from wallet.service import WalletService

router = APIRouter(prefix="/pools", tags=["pools"])
logger = logging.getLogger("matchiq.pool.api")


@router.get("/rules")
def pool_rules():
    """Static tiers / sizes / distribution used by the Create Pool UI."""
    sizes = []
    for players in rules.PLAYER_SIZES:
        winners = rules.winners_for(players)
        sizes.append(
            {
                "players": players,
                "winners": winners,
                "distribution": rules.distribution_percents(winners),
                "prizes": {
                    fee: rules.prize_pool(players, fee) for fee in rules.ENTRY_FEES
                },
            }
        )
    return {
        "prize_share": rules.PRIZE_SHARE,
        "platform_fee_share": rules.PLATFORM_FEE_SHARE,
        "entry_fees": list(rules.ENTRY_FEES),
        "player_sizes": list(rules.PLAYER_SIZES),
        "sizes": sizes,
        "notes": [
            "Prize Pool = 70% of total collection.",
            "Remaining 30% = platform fee + payment gateway + operations + promotions.",
            "Winners count scales with pool size.",
        ],
    }


@router.get("")
def list_pools(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    service = PoolService(db)
    pools = service.list_open_pools()
    # Attach the caller's entry (if any) to each pool.
    for pool in pools:
        entry = service._entry(pool["id"], user.id)
        pool["my_entry"] = service.serialize_entry(entry) if entry else None
    return {
        "wallet_balance": WalletService(db).get_balance(user.id),
        "pools": pools,
    }


@router.get("/history")
def pool_history(
    limit: int = Query(50, ge=1, le=200),
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    return {"history": PoolService(db).user_history(user.id, limit=limit)}


@router.get("/{pool_id}")
def pool_detail(
    pool_id: int,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    service = PoolService(db)
    pool = service.get_pool(pool_id)
    if not pool:
        raise HTTPException(status_code=404, detail="Pool not found")
    return service.serialize_pool(pool, user_id=user.id)


@router.post("/join")
def join_pool(
    payload: JoinPoolRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    service = PoolService(db)
    try:
        result = service.join_pool(user, payload.pool_id)
    except PoolError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    write_audit_log(
        db,
        action="pool_join",
        message=f"Joined pool {payload.pool_id}",
        actor_type="user",
        actor_id=user.user_uuid,
        target_type="pool",
        target_id=str(payload.pool_id),
    )
    return {
        "wallet_balance": WalletService(db).get_balance(user.id),
        "pool": result,
    }


@router.post("/submit-score")
def submit_pool_score(
    payload: SubmitPoolScoreRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    service = PoolService(db)
    try:
        result = service.submit_score(
            user,
            payload.pool_id,
            payload.score,
            moves=payload.moves,
            elapsed_seconds=payload.elapsed_seconds,
        )
    except PoolError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    return {
        "wallet_balance": WalletService(db).get_balance(user.id),
        "pool": result,
    }


@router.post("")
def create_pool(
    payload: CreatePoolRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    service = PoolService(db)
    try:
        pool = service.create_pool(
            entry_fee=payload.entry_fee,
            max_players=payload.max_players,
            name=payload.name,
            icon=payload.icon,
            winners_count=payload.winners_count,
            distribution=payload.distribution,
            created_by=user.user_uuid,
        )
    except PoolError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    write_audit_log(
        db,
        action="pool_create",
        message=f"Created pool {pool.id}",
        actor_type="user",
        actor_id=user.user_uuid,
        target_type="pool",
        target_id=str(pool.id),
    )
    return service.serialize_pool(pool, user_id=user.id)


@router.post("/{pool_id}/close")
def close_pool(
    pool_id: int,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    service = PoolService(db)
    try:
        result = service.close_pool(pool_id)
    except PoolError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    write_audit_log(
        db,
        action="pool_close",
        message=f"Closed pool {pool_id}",
        actor_type="user",
        actor_id=user.user_uuid,
        target_type="pool",
        target_id=str(pool_id),
    )
    return result
