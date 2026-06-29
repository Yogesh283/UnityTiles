from datetime import datetime

from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from core.audit import write_audit_log, write_security_event
from database.connection import get_db
from database.models import RoomPlayer, TournamentResult, TournamentRoom, User
from models.schemas import (
    JoinTournamentRequest,
    RoomResponse,
    SubmitScoreRequest,
    SubmitScoreResponse,
    TournamentResponse,
)
from tournament.anti_cheat import AntiCheatError, ScoreSubmission, validate_score_submission
from tournament.broadcast import schedule_match_finished_broadcast
from tournament.catalog import TOURNAMENT_CATALOG, get_tournament
from tournament.prize_table import get_paid_rank_count
from tournament.room_manager import RoomManager

router = APIRouter(prefix="/tournaments", tags=["tournaments"])


@router.get("", response_model=list[TournamentResponse])
def list_tournaments():
    return [
        TournamentResponse(
            id=t.id,
            icon=t.icon,
            display_name=t.display_name,
            max_players=t.max_players,
            entry_fee=t.entry_fee,
            prize_pool=t.prize_pool,
            platform_fee=t.platform_fee,
            reward_info=t.reward_info,
            waiting_seconds=t.waiting_seconds,
            status_label=t.status_label,
        )
        for t in TOURNAMENT_CATALOG
    ]


@router.post("/join", response_model=RoomResponse)
def join_tournament(
    payload: JoinTournamentRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    tournament = get_tournament(payload.tournament_id)
    if not tournament:
        raise HTTPException(status_code=404, detail="Tournament not found")

    manager = RoomManager(db)
    try:
        room = manager.find_or_create_room(payload.tournament_id, user.id)
        manager.join_room(room, user.id, tournament)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    write_audit_log(
        db,
        action="tournament_join",
        message=f"Joined room {room.id}",
        actor_type="user",
        actor_id=user.user_uuid,
        target_type="room",
        target_id=room.id,
        context={"tournament_id": payload.tournament_id},
    )
    db.commit()

    count = db.query(RoomPlayer).filter(RoomPlayer.room_id == room.id).count()
    room = db.query(TournamentRoom).filter(TournamentRoom.id == room.id).first()
    return RoomResponse(
        room_id=room.id,
        tournament_id=room.tournament_id,
        level_index=room.level_index,
        level_seed=room.level_seed,
        status=room.status,
        player_count=count,
        max_players=room.max_players,
        waiting_seconds=tournament.waiting_seconds,
    )


@router.get("/rooms/{room_id}")
def room_snapshot(room_id: str, db: Session = Depends(get_db)):
    room = db.query(TournamentRoom).filter(TournamentRoom.id == room_id).first()
    if not room:
        raise HTTPException(status_code=404, detail="Room not found")

    players = db.query(RoomPlayer).filter(RoomPlayer.room_id == room_id).all()
    tournament = get_tournament(room.tournament_id)
    paid_slots = get_paid_rank_count(room.tournament_id) if tournament else 0
    submitted_count = sum(1 for p in players if p.submitted_at is not None)
    return {
        "room_id": room.id,
        "tournament_id": room.tournament_id,
        "level_index": room.level_index,
        "level_seed": room.level_seed,
        "status": room.status,
        "paid_winner_slots": paid_slots,
        "submitted_count": submitted_count,
        "players": [
            {
                "user_id": p.user_id,
                "score": p.score,
                "moves": p.moves,
                "elapsed_seconds": p.elapsed_seconds,
                "rank": p.rank,
                "is_connected": p.is_connected,
                "has_submitted": p.submitted_at is not None,
            }
            for p in players
        ],
    }


@router.post("/submit-score", response_model=SubmitScoreResponse)
def submit_score(
    payload: SubmitScoreRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    room = db.query(TournamentRoom).filter(TournamentRoom.id == payload.room_id).first()
    if not room:
        raise HTTPException(status_code=404, detail="Room not found")

    player = (
        db.query(RoomPlayer)
        .filter(RoomPlayer.room_id == payload.room_id, RoomPlayer.user_id == user.id)
        .first()
    )
    if not player:
        raise HTTPException(status_code=404, detail="Player not in room")

    submission = ScoreSubmission(
        score=payload.score,
        moves=payload.moves,
        elapsed_seconds=payload.elapsed_seconds,
        level_index=room.level_index,
        level_seed=room.level_seed,
    )
    try:
        validate_score_submission(room, player, submission)
    except AntiCheatError as exc:
        write_security_event(
            db,
            event_type="anti_cheat_reject",
            message=str(exc),
            user_id=user.id,
            severity="warning",
            context={
                "room_id": payload.room_id,
                "score": payload.score,
                "moves": payload.moves,
                "elapsed_seconds": payload.elapsed_seconds,
            },
        )
        db.commit()
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    now = datetime.utcnow()
    player.score = payload.score
    player.moves = payload.moves
    player.elapsed_seconds = payload.elapsed_seconds
    player.submitted_at = now
    player.finished_at = now
    db.commit()

    manager = RoomManager(db)
    results = manager.try_instant_finalize(payload.room_id)

    db.refresh(room)
    db.refresh(player)

    if results:
        schedule_match_finished_broadcast(payload.room_id, results)

    return SubmitScoreResponse(
        ok=True,
        finalized=results is not None,
        rank=player.rank,
        prize=player.prize or 0,
        room_status=room.status,
    )


@router.get("/history")
def history(user: User = Depends(get_current_user), db: Session = Depends(get_db)):
    rows = (
        db.query(TournamentResult)
        .filter(TournamentResult.user_id == user.id)
        .order_by(TournamentResult.created_at.desc())
        .limit(50)
        .all()
    )
    return [
        {
            "tournament_id": row.tournament_id,
            "room_id": row.room_id,
            "rank": row.rank,
            "score": row.score,
            "prize": row.prize,
            "created_at": row.created_at,
        }
        for row in rows
    ]
