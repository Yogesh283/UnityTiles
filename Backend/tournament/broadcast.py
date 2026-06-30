from __future__ import annotations

import asyncio
import logging
from typing import Any

from websocket.connection_manager import manager as ws_manager

logger = logging.getLogger("matchiq.tournament.broadcast")


async def broadcast_room_event(room_id: str, payload: dict[str, Any]) -> None:
    await ws_manager.broadcast(room_id, payload)


def schedule_room_broadcast(room_id: str, event: str, data: dict[str, Any] | None = None) -> None:
    message: dict[str, Any] = {"event": event, "room_id": room_id}
    if data:
        message.update(data)

    logger.info("broadcast room_id=%s event=%s", room_id, event)

    try:
        loop = asyncio.get_running_loop()
        loop.create_task(broadcast_room_event(room_id, message))
    except RuntimeError:
        asyncio.run(broadcast_room_event(room_id, message))


def schedule_match_finished_broadcast(room_id: str, results: list[dict]) -> None:
    schedule_room_broadcast(
        room_id,
        "match_finished",
        {"results": results},
    )


def schedule_room_updated(room_id: str, room_payload: dict[str, Any]) -> None:
    schedule_room_broadcast(room_id, "room_updated", {"room": room_payload})


def schedule_player_joined(room_id: str, player: dict[str, Any], room_payload: dict[str, Any]) -> None:
    schedule_room_broadcast(
        room_id,
        "player_joined",
        {"player": player, "room": room_payload},
    )


def schedule_player_left(room_id: str, user_id: int, room_payload: dict[str, Any]) -> None:
    schedule_room_broadcast(
        room_id,
        "player_left",
        {"user_id": user_id, "room": room_payload},
    )


def schedule_match_start(room_id: str, room_payload: dict[str, Any]) -> None:
    schedule_room_broadcast(room_id, "match_start", {"room": room_payload})


def schedule_countdown(room_id: str, room_payload: dict[str, Any]) -> None:
    schedule_room_broadcast(room_id, "countdown", {"room": room_payload})
