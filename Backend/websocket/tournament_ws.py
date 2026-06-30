import json
from typing import Any

from fastapi import APIRouter, WebSocket, WebSocketDisconnect, status
from sqlalchemy.orm import Session

from auth.providers import decode_token, get_user_by_uuid
from database.connection import SessionLocal
from database.models import RoomPlayer, TournamentRoom, User
from tournament.room_manager import RoomManager
from tournament.room_state import serialize_room

router = APIRouter()


class RoomConnectionManager:
    def __init__(self) -> None:
        self.active: dict[str, set[WebSocket]] = {}
        self.socket_users: dict[WebSocket, int] = {}

    async def connect(self, room_id: str, websocket: WebSocket, user_id: int) -> None:
        await websocket.accept()
        self.active.setdefault(room_id, set()).add(websocket)
        self.socket_users[websocket] = user_id

    def disconnect(self, room_id: str, websocket: WebSocket) -> None:
        self.socket_users.pop(websocket, None)
        if room_id in self.active:
            self.active[room_id].discard(websocket)
            if not self.active[room_id]:
                del self.active[room_id]

    async def broadcast(self, room_id: str, message: dict[str, Any]) -> None:
        for connection in list(self.active.get(room_id, set())):
            try:
                await connection.send_text(json.dumps(message))
            except Exception:
                self.disconnect(room_id, connection)


manager = RoomConnectionManager()


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


@router.websocket("/ws/tournament/{room_id}")
async def tournament_room_ws(websocket: WebSocket, room_id: str, token: str | None = None) -> None:
    user_id = _resolve_user_id(token)
    if not user_id:
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
            await websocket.close(code=status.WS_1008_POLICY_VIOLATION)
            return

        await manager.connect(room_id, websocket, user_id)
        RoomManager(db).set_player_connected(room_id, user_id, True)
        await websocket.send_text(json.dumps({"event": "room_updated", "room": serialize_room(db, room)}))

        while True:
            raw = await websocket.receive_text()
            payload = json.loads(raw)
            event = payload.get("event")

            if event == "ping":
                await websocket.send_text(json.dumps({"event": "pong"}))
                continue

            if event == "room_state":
                room = db.query(TournamentRoom).filter(TournamentRoom.id == room_id).first()
                if room:
                    await websocket.send_text(
                        json.dumps({"event": "room_updated", "room": serialize_room(db, room)})
                    )
    except WebSocketDisconnect:
        manager.disconnect(room_id, websocket)
        RoomManager(db).set_player_connected(room_id, user_id, False)
    finally:
        db.close()
