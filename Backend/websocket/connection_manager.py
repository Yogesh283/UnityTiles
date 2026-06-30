from __future__ import annotations

import json
from typing import Any

from fastapi import WebSocket


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
