from datetime import datetime, timedelta

from jose import JWTError, jwt
from passlib.context import CryptContext
from sqlalchemy.orm import Session

from config import get_settings
from core.identifiers import new_user_uuid
from database.models import User, Wallet

pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")
settings = get_settings()


def hash_password(password: str) -> str:
    return pwd_context.hash(password)


def verify_password(password: str, password_hash: str) -> bool:
    return pwd_context.verify(password, password_hash)


def create_access_token(user_uuid: str) -> str:
    expire = datetime.utcnow() + timedelta(minutes=settings.jwt_expire_minutes)
    payload = {"sub": user_uuid, "exp": expire}
    return jwt.encode(payload, settings.jwt_secret, algorithm=settings.jwt_algorithm)


def decode_token(token: str) -> str | None:
    try:
        payload = jwt.decode(token, settings.jwt_secret, algorithms=[settings.jwt_algorithm])
        sub = payload.get("sub")
        return str(sub) if sub else None
    except (JWTError, TypeError, ValueError):
        return None


def _ensure_uuid(user: User) -> None:
    if not user.user_uuid:
        user.user_uuid = new_user_uuid()


def register_user(db: Session, email: str, password: str, display_name: str) -> User:
    user = User(
        user_uuid=new_user_uuid(),
        email=email,
        password_hash=hash_password(password),
        display_name=display_name,
        is_guest=False,
    )
    db.add(user)
    db.flush()
    db.add(Wallet(user_id=user.id, balance=500))
    db.commit()
    db.refresh(user)
    return user


def login_user(db: Session, email: str, password: str) -> User | None:
    user = db.query(User).filter(User.email == email).first()
    if not user or not user.password_hash:
        return None
    if not verify_password(password, user.password_hash):
        return None
    _ensure_uuid(user)
    db.commit()
    return user


def guest_login(db: Session, guest_id: str, display_name: str = "Guest") -> User:
    user = db.query(User).filter(User.guest_id == guest_id).first()
    if user:
        _ensure_uuid(user)
        db.commit()
        return user

    user = User(
        user_uuid=new_user_uuid(),
        guest_id=guest_id,
        display_name=display_name,
        is_guest=True,
    )
    db.add(user)
    db.flush()
    db.add(Wallet(user_id=user.id, balance=500))
    db.commit()
    db.refresh(user)
    return user


def google_login(db: Session, google_id: str, email: str, display_name: str) -> User:
    user = db.query(User).filter(User.google_id == google_id).first()
    if user:
        _ensure_uuid(user)
        db.commit()
        return user

    user = User(
        user_uuid=new_user_uuid(),
        google_id=google_id,
        email=email,
        display_name=display_name,
        is_guest=False,
    )
    db.add(user)
    db.flush()
    db.add(Wallet(user_id=user.id, balance=500))
    db.commit()
    db.refresh(user)
    return user


def get_user_by_uuid(db: Session, user_uuid: str) -> User | None:
    return db.query(User).filter(User.user_uuid == user_uuid).first()
