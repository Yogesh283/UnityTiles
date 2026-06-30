from __future__ import annotations

import asyncio

from database.connection import SessionLocal
from tournament.room_manager import RoomManager


async def room_scheduler_loop() -> None:
    while True:
        await asyncio.sleep(0.5)
        db = SessionLocal()
        try:
            RoomManager(db).tick_rooms()
        except Exception as exc:
            print(f"[room_scheduler] tick error: {exc}")
        finally:
            db.close()


def start_room_scheduler() -> None:
    asyncio.get_event_loop().create_task(room_scheduler_loop())
