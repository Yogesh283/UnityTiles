from datetime import date, datetime

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


def _parse_day(value: str | None) -> date | None:
    if not value:
        return None
    try:
        return datetime.strptime(value.strip(), "%Y-%m-%d").date()
    except ValueError:
        return None


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

    snap = service.team_snapshot(user)
    summary = service.income_summary(user.id)
    counts = snap["counts"]

    return {
        "referral_code": code,
        "referral_link": service.referral_link(code),
        "direct_count": snap["direct_count"],
        "sat_count": snap["sat_count"],
        "team_size": snap["team_size"],
        "team_counts": [{"level": lvl, "members": counts.get(lvl, 0)} for lvl in range(1, MAX_LEVEL + 1)],
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
    date_str: str | None = Query(None, alias="date"),
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    day = _parse_day(date_str)
    return {"items": ReferralService(db).recent_earnings(user.id, limit=limit, day=day)}


@router.get("/rewards")
def rewards(
    date_str: str | None = Query(None, alias="date"),
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    """All income for a calendar day. Today total is always included."""
    day = _parse_day(date_str) or date.today()
    return ReferralService(db).rewards_report(user, day)


@router.get("/directs")
def directs(
    limit: int = Query(100, ge=1, le=200),
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    """Direct referral list plus direct/total team counts for the current user."""
    service = ReferralService(db)
    service.ensure_code(user)
    db.commit()

    snap = service.team_snapshot(user)
    return {
        "direct_count": snap["direct_count"],
        "sat_count": snap["sat_count"],
        "team_size": snap["team_size"],
        "referral_code": user.referral_code,
        "referral_link": service.referral_link(user.referral_code or ""),
        "members": service.direct_members(user, limit=limit),
    }


@router.get("/team")
def team(
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    service = ReferralService(db)
    service.ensure_code(user)
    db.commit()
    snap = service.team_snapshot(user)
    counts = snap["counts"]
    summary = service.income_summary(user.id)
    by_level = {row["level"]: row for row in summary["by_level"]}
    structure = service.members_by_level(user)
    people_by_level = {row["level"]: row.get("members") or [] for row in structure}
    return {
        "referral_code": user.referral_code,
        "referral_link": service.referral_link(user.referral_code or ""),
        "direct_count": snap["direct_count"],
        "sat_count": snap["sat_count"],
        "total_members": snap["team_size"],
        "total_points": summary["total"],
        "direct_points": summary.get("direct", 0),
        "sat_points": summary.get("sat", 0),
        "levels": [
            {
                "level": lvl,
                "percent": LEVEL_PERCENTS[lvl],
                "label": "Direct" if lvl == 1 else f"Sat L{lvl}",
                "members": counts.get(lvl, 0),
                "points": by_level.get(lvl, {}).get("points", 0),
                "entries": by_level.get(lvl, {}).get("entries", 0),
                "people": people_by_level.get(lvl, []),
            }
            for lvl in range(1, MAX_LEVEL + 1)
        ],
    }


@router.post("/claim")
def claim_referral(
    body: dict,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    """Attach a sponsor after signup if the user is not already in a team."""
    code = str(body.get("referral_code") or body.get("code") or "").strip()
    service = ReferralService(db)
    if user.referred_by:
        sponsor = db.query(User).filter(User.id == user.referred_by).first()
        return {
            "attached": False,
            "already": True,
            "sponsor_id": user.referred_by,
            "sponsor_name": (sponsor.display_name if sponsor else None),
        }
    attached = service.attach_sponsor(user, code)
    if attached:
        db.commit()
        db.refresh(user)
        from wallet.service import WalletService

        WalletService(db).credit_direct_referral_bonus(user.referred_by, user.id)
    else:
        db.rollback()
    return {"attached": attached, "already": False, "sponsor_id": user.referred_by}
