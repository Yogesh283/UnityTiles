from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from database.connection import get_db
from database.models import User, WalletTransaction
from models.schemas import WalletResponse
from wallet.service import WalletService

router = APIRouter(prefix="/wallet", tags=["wallet"])


@router.get("/balance", response_model=WalletResponse)
def balance(user: User = Depends(get_current_user), db: Session = Depends(get_db)):
    return WalletResponse(balance=WalletService(db).get_balance(user.id))


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
