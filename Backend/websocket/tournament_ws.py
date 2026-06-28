import json
from typing import Any

from fastapi import APIRouter, WebSocket, WebSocketDisconnect
from sqlalchemy.orm import Session

from database.connection import SessionLocal
from database.models import RoomPlayer, TournamentRoom
from tournament.catalog import get_tournament
from tournament.room_manager import RoomManager

router = APIRouter()


class RoomConnectionManager:
    def __init__(self) -> None:
        self.active: dict[str, set[WebSocket]] = {}

    async def connect(self, room_id: str, websocket: WebSocket) -> None:
        await websocket.accept()
        self.active.setdefault(room_id, set()).add(websocket)

    def disconnect(self, room_id: str, websocket: WebSocket) -> None:
        if room_id in self.active:
            self.active[room_id].discard(websocket)
            if not self.active[room_id]:
                del self.active[room_id]

    async def broadcast(self, room_id: str, message: dict[str, Any]) -> None:
        for connection in list(self.active.get(room_id, set())):
            await connection.send_text(json.dumps(message))


manager = RoomConnectionManager()


@router.websocket("/ws/tournament/{room_id}")
async def tournament_room_ws(websocket: WebSocket, room_id: str) -> None:
    await manager.connect(room_id, websocket)
    db: Session = SessionLocal()
    try:
        while True:
            raw = await websocket.receive_text()
            payload = json.loads(raw)
            event = payload.get("event")

            if event == "ping":
                await websocket.send_text(json.dumps({"event": "pong"}))
                continue

            if event == "room_state":
                room = db.query(TournamentRoom).filter(TournamentRoom.id == room_id).first()
                players = db.query(RoomPlayer).filter(RoomPlayer.room_id == room_id).all()
                await websocket.send_text(
                    json.dumps(
                        {
                            "event": "room_state",
                            "room_id": room_id,
                            "status": room.status if room else "unknown",
                            "player_count": len(players),
                            "players": [{"user_id": p.user_id, "score": p.score} for p in players],
                        }
                    )
                )
                continue

            if event == "finalize":
                results = RoomManager(db).finalize_room(room_id)
                message = {"event": "match_finished", "room_id": room_id, "results": results}
                await manager.broadcast(room_id, message)
    except WebSocketDisconnect:
        manager.disconnect(room_id, websocket)
    finally:
        db.close()
