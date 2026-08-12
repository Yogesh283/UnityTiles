from datetime import datetime

from fastapi import APIRouter, Depends, HTTPException
from pydantic import BaseModel, Field
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from database.connection import get_db
from database.models import SupportTicket, User

router = APIRouter(prefix="/tickets", tags=["support"])

CATEGORIES = ("general", "payment", "game", "account", "other")
OPEN_STATUSES = ("open", "answered")
MAX_OPEN = 8
MAX_PER_DAY = 20


class TicketCreateRequest(BaseModel):
    subject: str = Field(min_length=4, max_length=160)
    message: str = Field(min_length=10, max_length=2000)
    category: str = Field(default="general", max_length=64)


def _ticket_code() -> str:
    import secrets

    return "ST-" + secrets.token_hex(3).upper()


def _iso(value: datetime | None) -> str | None:
    if not value:
        return None
    if value.tzinfo is None:
        return value.isoformat() + "Z"
    return value.isoformat()


def _public(row: SupportTicket) -> dict:
    return {
        "id": row.id,
        "ticket_code": row.ticket_code,
        "category": row.category,
        "subject": row.subject,
        "message": row.message,
        "status": row.status,
        "admin_reply": row.admin_reply,
        "created_at": _iso(row.created_at),
        "updated_at": _iso(row.updated_at),
        "reviewed_at": _iso(row.reviewed_at),
    }


def _normalize_category(raw: str) -> str:
    cat = (raw or "general").strip().lower()
    return cat if cat in CATEGORIES else "general"


@router.post("")
def create_ticket(
    body: TicketCreateRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    if user.is_guest:
        raise HTTPException(status_code=403, detail="Login with a real account to contact support.")
    if user.is_banned:
        raise HTTPException(status_code=403, detail="This account cannot send tickets.")

    open_count = (
        db.query(SupportTicket)
        .filter(SupportTicket.user_id == user.id, SupportTicket.status.in_(OPEN_STATUSES))
        .count()
    )
    if open_count >= MAX_OPEN:
        raise HTTPException(
            status_code=400,
            detail="You already have too many open tickets. Wait for a reply or close an old one.",
        )

    today_count = (
        db.query(SupportTicket)
        .filter(
            SupportTicket.user_id == user.id,
            SupportTicket.created_at >= datetime.utcnow().replace(hour=0, minute=0, second=0, microsecond=0),
        )
        .count()
    )
    if today_count >= MAX_PER_DAY:
        raise HTTPException(status_code=429, detail="Daily ticket limit reached. Try again tomorrow.")

    subject = " ".join(body.subject.strip().split())
    message = body.message.strip()
    if not subject or not message:
        raise HTTPException(status_code=400, detail="Subject and message are required.")

    row = SupportTicket(
        ticket_code=_ticket_code(),
        user_id=user.id,
        category=_normalize_category(body.category),
        subject=subject,
        message=message,
        status="open",
    )
    db.add(row)
    db.commit()
    db.refresh(row)
    return _public(row)


@router.get("")
def list_tickets(user: User = Depends(get_current_user), db: Session = Depends(get_db)):
    rows = (
        db.query(SupportTicket)
        .filter(SupportTicket.user_id == user.id)
        .order_by(SupportTicket.created_at.desc())
        .limit(50)
        .all()
    )
    return [_public(row) for row in rows]


@router.get("/{ticket_code}")
def get_ticket(
    ticket_code: str,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    row = (
        db.query(SupportTicket)
        .filter(SupportTicket.ticket_code == ticket_code, SupportTicket.user_id == user.id)
        .first()
    )
    if not row:
        raise HTTPException(status_code=404, detail="Ticket not found")
    return _public(row)


@router.post("/{ticket_code}/close")
def close_ticket(
    ticket_code: str,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    row = (
        db.query(SupportTicket)
        .filter(SupportTicket.ticket_code == ticket_code, SupportTicket.user_id == user.id)
        .first()
    )
    if not row:
        raise HTTPException(status_code=404, detail="Ticket not found")
    if row.status == "closed":
        return _public(row)
    row.status = "closed"
    db.commit()
    db.refresh(row)
    return _public(row)
