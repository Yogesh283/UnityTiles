from fastapi import APIRouter, Depends
from pydantic import BaseModel, Field
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from database.connection import get_db
from database.models import Banner, FcmToken, Notification, User

router = APIRouter(tags=["content"])


class FcmRegisterRequest(BaseModel):
    token: str = Field(min_length=10, max_length=512)
    platform: str = Field(default="android", max_length=32)


@router.get("/notifications")
def notifications(user: User = Depends(get_current_user), db: Session = Depends(get_db)):
    rows = (
        db.query(Notification)
        .filter((Notification.user_id == user.id) | (Notification.user_id.is_(None)))
        .order_by(Notification.created_at.desc())
        .limit(30)
        .all()
    )
    return [
        {
            "id": row.id,
            "title": row.title,
            "body": row.body,
            "is_read": row.is_read,
            "created_at": row.created_at,
        }
        for row in rows
    ]


@router.post("/notifications/fcm/register")
def register_fcm_token(
    body: FcmRegisterRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    existing = db.query(FcmToken).filter(FcmToken.token == body.token).first()
    if existing:
        existing.user_id = user.id
        existing.platform = body.platform
    else:
        db.add(FcmToken(user_id=user.id, token=body.token, platform=body.platform))
    db.commit()
    return {"ok": True}


@router.get("/banners")
def banners(db: Session = Depends(get_db)):
    rows = (
        db.query(Banner)
        .filter(Banner.is_active.is_(True))
        .order_by(Banner.sort_order.asc())
        .all()
    )
    return [
        {
            "id": row.id,
            "title": row.title,
            "image_url": row.image_url,
            "link_url": row.link_url,
        }
        for row in rows
    ]
