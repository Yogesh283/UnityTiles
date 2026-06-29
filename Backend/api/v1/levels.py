from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from core.identifiers import new_transaction_id
from database.connection import get_db
from database.models import User
from models.schemas import LevelCompleteRequest, LevelCompleteResponse
from wallet.service import WalletService

router = APIRouter(prefix="/levels", tags=["levels"])

REWARD_COINS = 50
MIN_LEVEL = 1
MAX_LEVEL = 300


@router.post("/complete", response_model=LevelCompleteResponse)
def complete_level(
    body: LevelCompleteRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    if not user.user_uuid or body.user_uuid != user.user_uuid:
        raise HTTPException(status_code=403, detail="User UUID mismatch")

    if body.level_number < MIN_LEVEL or body.level_number > MAX_LEVEL:
        raise HTTPException(status_code=400, detail="Invalid level number")

    wallet_service = WalletService(db)
    wallet_service.ensure_wallet(user.id)
    wallet = wallet_service.credit_level_complete(
        user.id,
        REWARD_COINS,
        body.level_number,
        completion_id=new_transaction_id(),
    )
    return LevelCompleteResponse(
        reward_given=True,
        reward_coins=REWARD_COINS,
        current_wallet_balance=wallet.balance,
    )
