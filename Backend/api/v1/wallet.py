from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from database.connection import get_db
from database.models import User, WalletTransaction
from models.schemas import P2PTransferRequest, WalletResponse, WithdrawRequest
from wallet.service import WalletService

router = APIRouter(prefix="/wallet", tags=["wallet"])


def _public_user(user: User) -> dict:
    return {
        "user_id": user.id,
        "display_name": user.display_name or "Player",
        "username": user.username,
        "referral_code": user.referral_code,
    }


@router.get("/balance", response_model=WalletResponse)
def balance(user: User = Depends(get_current_user), db: Session = Depends(get_db)):
    snap = WalletService(db).snapshot(user.id)
    return WalletResponse(balance=snap["balance"], bonus_balance=snap["bonus_balance"])


@router.get("/transactions")
def transactions(user: User = Depends(get_current_user), db: Session = Depends(get_db)):
    rows = (
        db.query(WalletTransaction)
        .filter(WalletTransaction.user_id == user.id)
        .order_by(WalletTransaction.created_at.desc())
        .limit(50)
        .all()
    )
    return [
        {
            "id": row.id,
            "transaction_id": row.transaction_id,
            "amount": row.amount,
            "balance_before": row.balance_before,
            "balance_after": row.balance_after,
            "type": row.type,
            "reference_id": row.reference_id,
            "reason": row.reason or row.note,
            "created_at": row.created_at,
        }
        for row in rows
    ]


@router.get("/p2p/lookup")
def p2p_lookup(
    q: str = Query(..., min_length=1, max_length=64),
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    target = WalletService(db).find_transfer_user(q)
    if not target:
        raise HTTPException(status_code=404, detail="User not found")
    if target.id == user.id:
        raise HTTPException(status_code=400, detail="You cannot transfer to yourself")
    if not target.is_active or target.is_banned:
        raise HTTPException(status_code=400, detail="This user cannot receive transfers")
    return _public_user(target)


@router.post("/p2p/transfer")
def p2p_transfer(
    payload: P2PTransferRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    service = WalletService(db)
    target = service.find_transfer_user(payload.to)
    if not target:
        raise HTTPException(status_code=404, detail="User not found")
    try:
        result = service.p2p_transfer(user, target, payload.amount, payload.note)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc
    return result


@router.get("/withdraw/methods")
def withdraw_methods(db: Session = Depends(get_db)):
    return WalletService(db).withdraw_methods()


@router.post("/withdraw")
def request_withdraw(
    payload: WithdrawRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    try:
        return WalletService(db).request_usdt_withdraw(user, payload.amount, payload.bep20_address)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@router.get("/withdrawals")
def withdrawals(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    return {"items": WalletService(db).list_withdrawals(user.id)}


@router.get("/p2p/history")
def p2p_history(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    rows = (
        db.query(WalletTransaction)
        .filter(
            WalletTransaction.user_id == user.id,
            WalletTransaction.type.in_(("p2p_send", "p2p_receive")),
        )
        .order_by(WalletTransaction.created_at.desc())
        .limit(50)
        .all()
    )
    other_ids = []
    for row in rows:
        try:
            other_ids.append(int(row.reference_id))
        except (TypeError, ValueError):
            continue
    names = {}
    if other_ids:
        found = db.query(User).filter(User.id.in_(set(other_ids))).all()
        names = {u.id: (u.display_name or "Player") for u in found}

    items = []
    for row in rows:
        try:
            other_id = int(row.reference_id) if row.reference_id else None
        except ValueError:
            other_id = None
        items.append(
            {
                "id": row.id,
                "type": row.type,
                "amount": abs(int(row.amount or 0)),
                "direction": "out" if row.type == "p2p_send" else "in",
                "other_user_id": other_id,
                "other_name": names.get(other_id, "Player") if other_id else "Player",
                "note": row.note or row.reason,
                "created_at": row.created_at,
            }
        )
    return {"items": items}
