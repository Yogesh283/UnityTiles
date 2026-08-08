from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from database.models import User
from database.connection import get_db
from referral.service import LEVEL_PERCENTS, MAX_LEVEL, TOTAL_PERCENT, ReferralService

router = APIRouter(prefix="/referral", tags=["referral"])

_LEVEL_LABELS = {
    1: "Direct Referrals",
    2: "2nd-Level Team",
    3: "3rd-Level Team",
    4: "4th-Level Team",
    5: "5th-Level Team",
    6: "6th-Level Team",
}


@router.get("/program")
def program():
    """Static WXO reward program definition (no auth required)."""
    return {
        "name": "WXO 6-Level Tournament Rewards Program",
        "max_level": MAX_LEVEL,
        "total_percent": TOTAL_PERCENT,
        "levels": [
            {
                "level": level,
                "percent": percent,
                "label": _LEVEL_LABELS.get(level, f"Level {level} Team"),
            }
            for level, percent in sorted(LEVEL_PERCENTS.items())
        ],
        "notes": [
            "Referral registration alone does not generate rewards.",
            "WXO Points are earned only when a downline member successfully pays an eligible tournament entry fee.",
            "The reward percentage is determined by the member's level within your referral network.",
        ],
    }


@router.get("/me")
def my_referral(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    service = ReferralService(db)
    code = service.ensure_code(user)
    db.commit()

    counts = service.team_counts(user)
    summary = service.income_summary(user.id)

    return {
        "referral_code": code,
        "referral_link": f"https://matchiq.fun/join?ref={code}",
        "team_counts": [{"level": lvl, "members": counts.get(lvl, 0)} for lvl in range(1, MAX_LEVEL + 1)],
        "team_size": sum(counts.values()),
        "income": summary,
    }


@router.get("/income")
def income(
    period: str = Query("today", pattern="^(today|week|month|total)$"),
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    summary = ReferralService(db).income_summary(user.id)
    return {
        "period": period,
        "points": summary.get(period, 0),
        "today": summary["today"],
        "week": summary["week"],
        "month": summary["month"],
        "total": summary["total"],
        "by_level": summary["by_level"],
    }


@router.get("/earnings")
def earnings(
    limit: int = Query(50, ge=1, le=200),
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    return {"items": ReferralService(db).recent_earnings(user.id, limit=limit)}


@router.get("/team")
def team(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    service = ReferralService(db)
    counts = service.team_counts(user)
    summary = service.income_summary(user.id)
    by_level = {row["level"]: row for row in summary["by_level"]}
    return {
        "levels": [
            {
                "level": lvl,
                "percent": LEVEL_PERCENTS[lvl],
                "members": counts.get(lvl, 0),
                "points": by_level.get(lvl, {}).get("points", 0),
                "entries": by_level.get(lvl, {}).get("entries", 0),
            }
            for lvl in range(1, MAX_LEVEL + 1)
        ],
        "total_members": sum(counts.values()),
        "total_points": summary["total"],
    }
