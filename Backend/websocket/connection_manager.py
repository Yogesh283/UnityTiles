from __future__ import annotations

import json
import logging
from typing import Any

from fastapi import WebSocket

logger = logging.getLogger("matchiq.tournament.ws.manager")


class RoomConnectionManager:
    def __init__(self) -> None:
        self.active: dict[str, set[WebSocket]] = {}
        self.socket_users: dict[WebSocket, int] = {}
        self.user_socket: dict[tuple[str, int], WebSocket] = {}

    def subscriber_count(self, room_id: str) -> int:
        return len(self.active.get(room_id, set()))

    async def connect(self, room_id: str, websocket: WebSocket, user_id: int) -> None:
        await websocket.accept()

        key = (room_id, user_id)
        previous = self.user_socket.get(key)
        if previous is not None and previous is not websocket:
            logger.info(
                "[TournamentWsProbe] Replacing stale socket room=%s user_id=%s",
                room_id,
                user_id,
            )
            try:
                await previous.close(code=1000, reason="replaced_by_new_connection")
            except Exception:
                pass
            self.disconnect(room_id, previous)

        self.active.setdefault(room_id, set()).add(websocket)
        self.socket_users[websocket] = user_id
        self.user_socket[key] = websocket
        logger.info(
            "[TournamentWsProbe] WebSocket CONNECTED room=%s user_id=%s subscribers=%s",
            room_id,
            user_id,
            self.subscriber_count(room_id),
        )

    def disconnect(self, room_id: str, websocket: WebSocket) -> None:
        user_id = self.socket_users.pop(websocket, None)
        if user_id is not None:
            key = (room_id, user_id)
            if self.user_socket.get(key) is websocket:
                del self.user_socket[key]
        if room_id in self.active:
            self.active[room_id].discard(websocket)
            if not self.active[room_id]:
                del self.active[room_id]
        logger.info(
            "[TournamentWsProbe] Disconnect reason=manager_remove room=%s user_id=%s subscribers=%s",
            room_id,
            user_id,
            self.subscriber_count(room_id),
        )

    async def broadcast(self, room_id: str, message: dict[str, Any]) -> None:
        event = message.get("event", "?")
        targets = list(self.active.get(room_id, set()))
        logger.info(
            "[TournamentWsProbe] Broadcast %s room=%s target_sockets=%s",
            event,
            room_id,
            len(targets),
        )
        sent = 0
        failed = 0
        for connection in targets:
            try:
                await connection.send_text(json.dumps(message))
                sent += 1
            except Exception as exc:
                failed += 1
                user_id = self.socket_users.get(connection)
                logger.warning(
                    "[TournamentWsProbe] Broadcast %s FAILED room=%s user_id=%s error=%s",
                    event,
                    room_id,
                    user_id,
                    exc,
                )
                self.disconnect(room_id, connection)
        logger.info(
            "[TournamentWsProbe] Broadcast %s room=%s sent=%s failed=%s remaining=%s",
            event,
            room_id,
            sent,
            failed,
            self.subscriber_count(room_id),
        )


manager = RoomConnectionManager()
