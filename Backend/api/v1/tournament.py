import logging

from datetime import datetime

from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from core.audit import write_audit_log, write_security_event
from database.connection import get_db
from database.models import RoomPlayer, TournamentResult, TournamentRoom, User
from models.schemas import (
    JoinTournamentRequest,
    RoomPlayerResponse,
    RoomResponse,
    SubmitScoreRequest,
    SubmitScoreResponse,
    TournamentLevelRewardRequest,
    TournamentResponse,
    WalletResponse,
)
from tournament.anti_cheat import AntiCheatError, ScoreSubmission, validate_score_submission
from tournament.catalog import TOURNAMENT_CATALOG, get_tournament
from tournament.prize_table import get_paid_rank_count
from tournament.room_manager import RoomManager
from tournament.room_state import serialize_player, serialize_room
from wallet.service import WalletService

router = APIRouter(prefix="/tournaments", tags=["tournaments"])
logger = logging.getLogger("matchiq.tournament.api")


def _build_room_response(
    db: Session,
    room: TournamentRoom,
    *,
    wallet_balance: int | None = None,
) -> RoomResponse:
    payload = serialize_room(db, room)
    return RoomResponse(
        room_id=payload["room_id"],
        tournament_id=payload["tournament_id"],
        tournament_name=payload["tournament_name"],
        level_index=payload["level_index"],
        level_seed=payload["level_seed"],
        status=payload["status"],
        player_count=payload["player_count"],
        max_players=payload["max_players"],
        waiting_seconds=payload["waiting_seconds"],
        waiting_seconds_remaining=payload["waiting_seconds_remaining"],
        start_countdown_seconds=payload["start_countdown_seconds"],
        match_start_at_ms=payload["match_start_at_ms"],
        server_now_ms=payload.get("server_now_ms"),
        search_status=payload.get("search_status"),
        queued=False,
        wallet_balance=wallet_balance,
        players=[RoomPlayerResponse(**player) for player in payload["players"]],
    )


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
    wallet = WalletService(db)

    already_in_room = (
        db.query(RoomPlayer)
        .join(TournamentRoom, TournamentRoom.id == RoomPlayer.room_id)
        .filter(
            RoomPlayer.user_id == user.id,
            TournamentRoom.tournament_id == payload.tournament_id,
            TournamentRoom.status.notin_(("finished",)),
        )
        .first()
    )
    if not already_in_room and wallet.get_balance(user.id) < tournament.entry_fee:
        raise HTTPException(status_code=400, detail="Insufficient balance")

    try:
        result = manager.matchmake(payload.tournament_id, user.id)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    room = result.room
    logger.info("join user_id=%s tournament=%s room_id=%s status=%s", user.id, payload.tournament_id, room.id, room.status)
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

    room = db.query(TournamentRoom).filter(TournamentRoom.id == room.id).first()
    wallet_balance = WalletService(db).get_balance(user.id)
    return _build_room_response(db, room, wallet_balance=wallet_balance)


@router.get("/rooms/{room_id}", response_model=RoomResponse)
def room_snapshot(room_id: str, db: Session = Depends(get_db)):
    room = db.query(TournamentRoom).filter(TournamentRoom.id == room_id).first()
    if not room:
        raise HTTPException(status_code=404, detail="Room not found")
    return _build_room_response(db, room)


@router.post("/submit-score", response_model=SubmitScoreResponse)
def submit_score(
    payload: SubmitScoreRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    room = db.query(TournamentRoom).filter(TournamentRoom.id == payload.room_id).first()
    if not room:
        raise HTTPException(status_code=404, detail="Room not found")

    if room.status in {"locked", "finished"}:
        raise HTTPException(status_code=409, detail="Match already ended")

    if room.status != "active":
        raise HTTPException(status_code=400, detail="Room is not active")

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

    if results is not None:
        logger.info("submit-score finalized room_id=%s user_id=%s rank=%s", payload.room_id, user.id, player.rank)
    else:
        logger.info("submit-score stored room_id=%s user_id=%s waiting_finalize", payload.room_id, user.id)

    return SubmitScoreResponse(
        ok=True,
        finalized=results is not None,
        rank=player.rank,
        prize=player.prize or 0,
        room_status=room.status,
    )


@router.post("/level-reward", response_model=WalletResponse)
def tournament_level_reward(
    body: TournamentLevelRewardRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    room = db.query(TournamentRoom).filter(TournamentRoom.id == body.room_id).first()
    if not room:
        raise HTTPException(status_code=404, detail="Room not found")

    player = (
        db.query(RoomPlayer)
        .filter(RoomPlayer.room_id == body.room_id, RoomPlayer.user_id == user.id)
        .first()
    )
    if not player:
        raise HTTPException(status_code=404, detail="Player not in room")

    if room.status not in {"active", "locked", "finished"}:
        raise HTTPException(status_code=400, detail="Room is not active")

    level_number = room.level_index + 1
    if level_number != 300:
        balance = WalletService(db).get_balance(user.id)
        return WalletResponse(balance=balance)

    wallet = WalletService(db).credit_tournament_level(user.id, 50, body.room_id)
    return WalletResponse(balance=wallet.balance)


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
