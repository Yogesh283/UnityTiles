from fastapi import Depends, HTTPException, Request, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from sqlalchemy.orm import Session

from auth.providers import decode_token, get_user_by_uuid
from core.redis_client import set_online_user
from core.security import SecurityService
from database.connection import get_db
from database.models import User

security = HTTPBearer(auto_error=False)


def _client_ip(request: Request) -> str | None:
    forwarded = request.headers.get("x-forwarded-for")
    if forwarded:
        return forwarded.split(",")[0].strip()
    if request.client:
        return request.client.host
    return None


def get_current_user(
    request: Request,
    credentials: HTTPAuthorizationCredentials | None = Depends(security),
    db: Session = Depends(get_db),
) -> User:
    if not credentials:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Not authenticated")

    user_uuid = decode_token(credentials.credentials)
    if not user_uuid:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid token")

    user = get_user_by_uuid(db, user_uuid)
    if not user:
        # Legacy token fallback: numeric internal id during migration
        if user_uuid.isdigit():
            user = db.query(User).filter(User.id == int(user_uuid), User.is_active.is_(True)).first()
        if not user:
            raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="User not found")

    ip_address = _client_ip(request)
    device_id = request.headers.get("x-device-id")
    sec = SecurityService(db)
    try:
        sec.assert_user_allowed(user, ip_address, device_id)
    except PermissionError as exc:
        raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail=str(exc)) from exc

    sec.touch_user_session(user, ip_address, device_id)
    if user.user_uuid:
        set_online_user(user.user_uuid)
    return user
