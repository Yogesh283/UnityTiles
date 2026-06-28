from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from auth.providers import (
    create_access_token,
    google_login,
    guest_login,
    login_user,
    register_user,
)
from core.audit import write_audit_log
from core.identifiers import new_user_uuid
from database.connection import get_db
from database.models import User
from models.schemas import (
    GoogleLoginRequest,
    GuestLoginRequest,
    LoginRequest,
    RegisterRequest,
    TokenResponse,
)

router = APIRouter(prefix="/auth", tags=["auth"])


def _token_response(db: Session, user: User) -> TokenResponse:
    if not user.user_uuid:
        user.user_uuid = new_user_uuid()
        db.commit()
        db.refresh(user)
    write_audit_log(
        db,
        action="login",
        message=f"User authenticated: {user.user_uuid}",
        actor_type="user",
        actor_id=user.user_uuid,
        target_type="user",
        target_id=user.user_uuid,
    )
    db.commit()
    return TokenResponse(
        access_token=create_access_token(user.user_uuid),
        user_id=user.id,
        user_uuid=user.user_uuid,
        display_name=user.display_name,
    )


@router.post("/register", response_model=TokenResponse)
def register(payload: RegisterRequest, db: Session = Depends(get_db)):
    existing = db.query(User).filter(User.email == payload.email).first()
    if existing:
        raise HTTPException(status_code=400, detail="Email already registered")
    user = register_user(db, payload.email, payload.password, payload.display_name)
    return _token_response(db, user)


@router.post("/login", response_model=TokenResponse)
def login(payload: LoginRequest, db: Session = Depends(get_db)):
    user = login_user(db, payload.email, payload.password)
    if not user:
        raise HTTPException(status_code=401, detail="Invalid credentials")
    return _token_response(db, user)


@router.post("/guest", response_model=TokenResponse)
def guest(payload: GuestLoginRequest, db: Session = Depends(get_db)):
    user = guest_login(db, payload.guest_id, payload.display_name)
    return _token_response(db, user)


@router.post("/google", response_model=TokenResponse)
def google(payload: GoogleLoginRequest, db: Session = Depends(get_db)):
    user = google_login(db, payload.google_id, payload.email, payload.display_name)
    return _token_response(db, user)


@router.get("/me")
def me(user: User = Depends(get_current_user)):
    return {
        "id": user.id,
        "user_uuid": user.user_uuid,
        "email": user.email,
        "display_name": user.display_name,
        "is_guest": user.is_guest,
        "avatar_url": user.avatar_url,
    }
