import json
import logging

from fastapi import APIRouter, WebSocket, WebSocketDisconnect, status
from sqlalchemy.orm import Session

from auth.providers import decode_token, get_user_by_uuid
from database.connection import SessionLocal
from database.models import RoomPlayer, TournamentRoom, User
from tournament.room_manager import RoomManager
from tournament.room_state import serialize_room
from websocket.connection_manager import manager

router = APIRouter()
logger = logging.getLogger("matchiq.tournament.ws")


def _resolve_user_id(token: str | None) -> int | None:
    if not token:
        return None
    db: Session = SessionLocal()
    try:
        user_uuid = decode_token(token)
        if not user_uuid:
            return None
        user = get_user_by_uuid(db, user_uuid)
        if not user and user_uuid.isdigit():
            user = db.query(User).filter(User.id == int(user_uuid)).first()
        return user.id if user else None
    finally:
        db.close()


def _log_room_payload(direction: str, room_id: str, user_id: int, event: str, room_payload: dict | None) -> None:
    logger.info(
        "[TournamentWsProbe] %s room=%s user_id=%s event=%s status=%s players=%s match_start_at_ms=%s",
        direction,
        room_id,
        user_id,
        event,
        room_payload.get("status") if room_payload else None,
        room_payload.get("player_count") if room_payload else None,
        room_payload.get("match_start_at_ms") if room_payload else None,
    )


@router.websocket("/ws/tournament/{room_id}")
async def tournament_room_ws(websocket: WebSocket, room_id: str, token: str | None = None) -> None:
    user_id = _resolve_user_id(token)
    if not user_id:
        logger.warning("[TournamentWsProbe] CONNECT rejected invalid token room=%s", room_id)
        await websocket.close(code=status.WS_1008_POLICY_VIOLATION)
        return

    db: Session = SessionLocal()
    try:
        room = db.query(TournamentRoom).filter(TournamentRoom.id == room_id).first()
        player = (
            db.query(RoomPlayer)
            .filter(RoomPlayer.room_id == room_id, RoomPlayer.user_id == user_id)
            .first()
        )
        if not room or not player:
            logger.warning(
                "[TournamentWsProbe] CONNECT rejected room=%s user_id=%s room_found=%s player_found=%s",
                room_id,
                user_id,
                bool(room),
                bool(player),
            )
            await websocket.close(code=status.WS_1008_POLICY_VIOLATION)
            return

        await manager.connect(room_id, websocket, user_id)
        logger.info("WebSocket /ws/tournament/%s [accepted] user_id=%s", room_id, user_id)
        RoomManager(db).set_player_connected(room_id, user_id, True)
        initial = serialize_room(db, room)
        _log_room_payload("Send initial", room_id, user_id, "room_updated", initial)
        await websocket.send_text(json.dumps({"event": "room_updated", "room": initial}))

        logger.info("[TournamentWsProbe] ReceiveLoop STARTED room=%s user_id=%s", room_id, user_id)
        while True:
            raw = await websocket.receive_text()
            payload = json.loads(raw)
            event = payload.get("event")

            if event == "ping":
                logger.info("[TournamentWsProbe] Ping received (app-level) room=%s user_id=%s", room_id, user_id)
                await websocket.send_text(json.dumps({"event": "pong"}))
                logger.info("[TournamentWsProbe] Pong sent (app-level) room=%s user_id=%s", room_id, user_id)
                continue

            if event == "room_state":
                room = db.query(TournamentRoom).filter(TournamentRoom.id == room_id).first()
                if room:
                    snap = serialize_room(db, room)
                    _log_room_payload("Send room_state reply", room_id, user_id, "room_updated", snap)
                    await websocket.send_text(
                        json.dumps({"event": "room_updated", "room": snap})
                    )
            else:
                logger.info(
                    "[TournamentWsProbe] Client message room=%s user_id=%s event=%s",
                    room_id,
                    user_id,
                    event,
                )
    except WebSocketDisconnect as exc:
        logger.info(
            "[TournamentWsProbe] ReceiveLoop EXITED room=%s user_id=%s reason=WebSocketDisconnect code=%s",
            room_id,
            user_id,
            getattr(exc, "code", None),
        )
        logger.info("WebSocket /ws/tournament/%s [disconnected] user_id=%s", room_id, user_id)
        manager.disconnect(room_id, websocket)
        RoomManager(db).set_player_connected(room_id, user_id, False)
    except Exception:
        logger.exception(
            "[TournamentWsProbe] ReceiveLoop EXITED room=%s user_id=%s reason=exception",
            room_id,
            user_id,
        )
        raise
    finally:
        db.close()
