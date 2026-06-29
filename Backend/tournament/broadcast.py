from __future__ import annotations

import asyncio
from typing import Any

from websocket.tournament_ws import manager as ws_manager


async def broadcast_match_finished(room_id: str, payload: dict[str, Any]) -> None:
    await ws_manager.broadcast(room_id, payload)


def schedule_match_finished_broadcast(room_id: str, results: list[dict]) -> None:
    message = {
        "event": "match_finished",
        "room_id": room_id,
        "results": results,
    }

    try:
        loop = asyncio.get_running_loop()
        loop.create_task(broadcast_match_finished(room_id, message))
    except RuntimeError:
        asyncio.run(broadcast_match_finished(room_id, message))
