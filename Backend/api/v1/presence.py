from fastapi import APIRouter, Depends

from auth.jwt import get_current_user
from core.redis_client import set_online_user
from database.models import User

router = APIRouter(prefix="/presence", tags=["presence"])


@router.post("/heartbeat")
def heartbeat(user: User = Depends(get_current_user)):
    if user.user_uuid:
        set_online_user(user.user_uuid, ttl_seconds=120)
    return {"ok": True}
