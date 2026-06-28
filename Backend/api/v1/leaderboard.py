from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from database.connection import get_db
from database.models import LeaderboardEntry, User

router = APIRouter(prefix="/leaderboard", tags=["leaderboard"])


@router.get("")
def leaderboard(limit: int = 50, db: Session = Depends(get_db)):
    rows = (
        db.query(LeaderboardEntry, User)
        .join(User, User.id == LeaderboardEntry.user_id)
        .order_by(LeaderboardEntry.total_prize.desc(), LeaderboardEntry.total_wins.desc())
        .limit(limit)
        .all()
    )
    return [
        {
            "user_id": user.id,
            "display_name": user.display_name,
            "total_wins": entry.total_wins,
            "total_prize": entry.total_prize,
            "tournaments_played": entry.tournaments_played,
            "best_rank": entry.best_rank,
        }
        for entry, user in rows
    ]
