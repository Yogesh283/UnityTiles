import base64
import hashlib
import hmac
import logging
import os
from datetime import datetime, timedelta

from jose import JWTError, jwt
from sqlalchemy.orm import Session

from config import get_settings
from core.identifiers import new_user_uuid
from database.models import User, Wallet

logger = logging.getLogger(__name__)
settings = get_settings()

PBKDF2_PREFIX = "pbkdf2_sha256"
PBKDF2_ITERATIONS = 260_000


def _load_bcrypt():
    """bcrypt is optional: some hosts ship a broken/partial native wheel."""
    try:
        import bcrypt as _bcrypt

        _bcrypt.checkpw(b"probe", _bcrypt.hashpw(b"probe", _bcrypt.gensalt(4)))
        return _bcrypt
    except Exception as exc:  # noqa: BLE001 - any failure means unusable
        logger.warning("bcrypt unavailable (%s); using pbkdf2_sha256 for passwords", exc)
        return None


_bcrypt = _load_bcrypt()


def _starting_coins() -> int:
    if settings.environment == "development":
        return 5000
    return 0


def _pbkdf2_hash(password: str, salt: bytes, iterations: int) -> str:
    digest = hashlib.pbkdf2_hmac("sha256", password.encode("utf-8"), salt, iterations)
    return "{}${}${}${}".format(
        PBKDF2_PREFIX,
        iterations,
        base64.b64encode(salt).decode("ascii"),
        base64.b64encode(digest).decode("ascii"),
    )


def _pbkdf2_verify(password: str, password_hash: str) -> bool:
    try:
        _, iterations, salt_b64, digest_b64 = password_hash.split("$", 3)
        expected = base64.b64decode(digest_b64)
        actual = hashlib.pbkdf2_hmac(
            "sha256",
            password.encode("utf-8"),
            base64.b64decode(salt_b64),
            int(iterations),
        )
        return hmac.compare_digest(expected, actual)
    except (ValueError, TypeError):
        return False


def hash_password(password: str) -> str:
    if _bcrypt is not None:
        return _bcrypt.hashpw(password.encode("utf-8"), _bcrypt.gensalt()).decode("utf-8")
    return _pbkdf2_hash(password, os.urandom(16), PBKDF2_ITERATIONS)


def verify_password(password: str, password_hash: str) -> bool:
    if password_hash.startswith(PBKDF2_PREFIX + "$"):
        return _pbkdf2_verify(password, password_hash)
    if _bcrypt is None:
        return False
    try:
        return _bcrypt.checkpw(
            password.encode("utf-8"),
            password_hash.encode("utf-8"),
        )
    except (ValueError, TypeError):
        return False


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


def _finalize_new_user(db: Session, user: User, referral_code: str | None) -> None:
    """Assign a WXO referral code and link the sponsor, if any."""
    from referral.service import ReferralService

    service = ReferralService(db)
    service.ensure_code(user)
    if referral_code:
        service.attach_sponsor(user, referral_code)


def register_user(
    db: Session,
    email: str,
    password: str,
    display_name: str,
    referral_code: str | None = None,
) -> User:
    user = User(
        user_uuid=new_user_uuid(),
        email=email,
        password_hash=hash_password(password),
        display_name=display_name,
        is_guest=False,
    )
    db.add(user)
    db.flush()
    db.add(Wallet(user_id=user.id, balance=_starting_coins(), bonus_balance=0))
    _finalize_new_user(db, user, referral_code)
    db.commit()
    from wallet.service import WalletService

    WalletService(db).credit_signup_bonus(user.id)
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


def _ensure_wallet(db: Session, user_id: int) -> None:
    wallet = db.query(Wallet).filter(Wallet.user_id == user_id).first()
    starting = _starting_coins()
    if not wallet:
        db.add(Wallet(user_id=user_id, balance=starting, bonus_balance=0))
    elif starting > 0 and wallet.balance < starting:
        wallet.balance = starting


def guest_login(
    db: Session,
    guest_id: str,
    display_name: str = "Guest",
    referral_code: str | None = None,
) -> User:
    user = db.query(User).filter(User.guest_id == guest_id).first()
    if user:
        _ensure_uuid(user)
        _ensure_wallet(db, user.id)
        _finalize_new_user(db, user, referral_code)
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
    db.add(Wallet(user_id=user.id, balance=_starting_coins(), bonus_balance=0))
    _finalize_new_user(db, user, referral_code)
    db.commit()
    db.refresh(user)
    return user


def google_login(
    db: Session,
    google_id: str,
    email: str,
    display_name: str,
    referral_code: str | None = None,
) -> User:
    user = db.query(User).filter(User.google_id == google_id).first()
    if user:
        _ensure_uuid(user)
        _finalize_new_user(db, user, referral_code)
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
    db.add(Wallet(user_id=user.id, balance=_starting_coins(), bonus_balance=0))
    _finalize_new_user(db, user, referral_code)
    db.commit()
    from wallet.service import WalletService

    WalletService(db).credit_signup_bonus(user.id)
    db.refresh(user)
    return user


def get_user_by_uuid(db: Session, user_uuid: str) -> User | None:
    return db.query(User).filter(User.user_uuid == user_uuid).first()
