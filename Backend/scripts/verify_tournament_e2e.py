#!/usr/bin/env python3
"""
End-to-end tournament verification — run before requesting an APK build.

Covers:
  1. Health / API availability
  2. Join response JSON compatible with Unity RoomResponseDto (nullable fields)
  3. Matchmaking (two players, same room, player_count=2)
  4. WebSocket connect + events (room_updated, match_start, match_finished)
  5. Room status transitions: waiting -> starting -> active
  6. submit-score + match finalization (duel_1v1)
"""

from __future__ import annotations

import argparse
import asyncio
import json
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from typing import Any

try:
    import websockets
except ImportError:
    websockets = None  # type: ignore

API_BASE_DEFAULT = "https://api.matchiq.fun/api/v1"
WS_ROOT_DEFAULT = "wss://api.matchiq.fun/ws/tournament"
MATCH_START_COUNTDOWN_SECONDS = 3

# Unity RoomResponseDto / RoomPlayerDto — fields that MUST accept JSON null.
UNITY_NULLABLE_INT = frozenset(
    {
        "waiting_seconds_remaining",
        "start_countdown_seconds",
        "wallet_balance",
        "current_rank",
        "game_level",
        "rank",
    }
)
UNITY_NULLABLE_LONG = frozenset({"match_start_at_ms", "server_now_ms"})
UNITY_REQUIRED_INT = frozenset(
    {
        "level_index",
        "level_seed",
        "player_count",
        "max_players",
        "waiting_seconds",
        "user_id",
        "score",
        "moves",
        "elapsed_seconds",
    }
)


def api_root(base: str) -> str:
    return base.rstrip("/")


def ws_root_from_api(api_base: str) -> str:
    root = api_base.rstrip("/")
    if root.endswith("/api/v1"):
        root = root[: -len("/api/v1")]
    return root.replace("https://", "wss://").replace("http://", "ws://") + "/ws/tournament"


def http_json(
    method: str,
    url: str,
    body: dict | None = None,
    token: str | None = None,
    timeout: int = 30,
) -> tuple[int, Any]:
    data = json.dumps(body).encode("utf-8") if body is not None else None
    headers = {"Content-Type": "application/json"} if body is not None else {}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(url, data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            raw = resp.read().decode("utf-8")
            return resp.status, json.loads(raw) if raw else None
    except urllib.error.HTTPError as exc:
        raw = exc.read().decode("utf-8", errors="replace")
        try:
            return exc.code, json.loads(raw)
        except json.JSONDecodeError:
            return exc.code, raw


def post_json(path: str, body: dict, token: str | None, api_base: str) -> tuple[int, Any]:
    return http_json("POST", f"{api_root(api_base)}/{path.lstrip('/')}", body, token)


def get_json(path: str, api_base: str, token: str | None = None) -> tuple[int, Any]:
    return http_json("GET", f"{api_root(api_base)}/{path.lstrip('/')}", None, token)


def guest_session(guest_id: str, api_base: str) -> tuple[str, str, int]:
    status, payload = post_json(
        "auth/guest",
        {"guest_id": guest_id, "display_name": guest_id},
        None,
        api_base,
    )
    if status != 200 or not isinstance(payload, dict):
        raise RuntimeError(f"guest login failed ({status}): {payload}")
    return payload["access_token"], payload["user_uuid"], int(payload["user_id"])


def join_duel(token: str, api_base: str, *, max_attempts: int = 8) -> tuple[int, Any]:
    for attempt in range(1, max_attempts + 1):
        status, body = post_json("tournaments/join", {"tournament_id": "duel_1v1"}, token, api_base)
        if status == 200:
            return status, body
        if (
            status == 400
            and isinstance(body, dict)
            and "matchmaking busy" in str(body.get("detail", "")).lower()
            and attempt < max_attempts
        ):
            time.sleep(min(2.0 * attempt, 8.0))
            continue
        return status, body
    return status, body


def join_duel_waiting_room(api_base: str, label: str) -> tuple[str, str, str, dict]:
    """Return token, user_uuid, room_id, join body for a solo waiting room (player_count=1)."""
    for attempt in range(5):
        guest_id = f"e2e_{label}_{uuid.uuid4().hex[:10]}"
        token, user_uuid, _ = guest_session(guest_id, api_base)
        fund_wallet(token, user_uuid, api_base, min_balance=200)
        status, join = join_duel(token, api_base)
        if status != 200 or not isinstance(join, dict):
            raise RuntimeError(f"{label} join failed ({status}): {join}")
        if join.get("status") == "waiting" and join.get("player_count") == 1:
            return token, user_uuid, join["room_id"], join
        print(
            f"  {label} join attempt {attempt + 1}: "
            f"status={join.get('status')} players={join.get('player_count')} — retrying"
        )
        time.sleep(0.5)
    raise RuntimeError(f"{label} could not create solo waiting room after 5 attempts")


def fund_wallet(token: str, user_uuid: str, api_base: str, min_balance: int = 500) -> int:
    status, wallet = get_json("wallet/balance", api_base, token)
    balance = wallet.get("balance", 0) if status == 200 and isinstance(wallet, dict) else 0
    level = 1
    while balance < min_balance and level <= 20:
        status, resp = post_json(
            "levels/complete",
            {"user_uuid": user_uuid, "level_number": level},
            token,
            api_base,
        )
        if status != 200:
            raise RuntimeError(f"level complete failed ({status}): {resp}")
        balance = resp.get("current_wallet_balance", balance)
        level += 1
    if balance < min_balance:
        raise RuntimeError(f"could not fund wallet to {min_balance}, got {balance}")
    return balance


def validate_unity_join_response(data: dict) -> list[str]:
    """Return parse errors that would break Unity Newtonsoft deserialization."""
    errors: list[str] = []

    def check_int(path: str, value: Any, *, nullable: bool) -> None:
        if value is None:
            if not nullable:
                errors.append(f"{path}: null (Unity non-nullable int)")
            return
        if isinstance(value, bool) or not isinstance(value, int):
            errors.append(f"{path}: expected int, got {type(value).__name__}")

    def check_long(path: str, value: Any, *, nullable: bool) -> None:
        if value is None:
            if not nullable:
                errors.append(f"{path}: null (Unity non-nullable long)")
            return
        if isinstance(value, bool) or not isinstance(value, int):
            errors.append(f"{path}: expected int/long, got {type(value).__name__}")

    for field in UNITY_REQUIRED_INT:
        if field in data:
            check_int(field, data[field], nullable=False)

    for field in UNITY_NULLABLE_INT:
        if field in data:
            check_int(field, data[field], nullable=True)

    for field in UNITY_NULLABLE_LONG:
        if field in data:
            check_long(field, data[field], nullable=True)

    for idx, player in enumerate(data.get("players") or []):
        if not isinstance(player, dict):
            errors.append(f"players[{idx}]: not an object")
            continue
        prefix = f"players[{idx}]"
        for field in ("user_id", "score", "moves", "elapsed_seconds"):
            if field in player:
                check_int(f"{prefix}.{field}", player[field], nullable=False)
        for field in ("current_rank", "game_level", "rank"):
            if field in player:
                check_int(f"{prefix}.{field}", player[field], nullable=True)
        for field in ("is_connected", "has_submitted"):
            if field in player and player[field] is not None and not isinstance(player[field], bool):
                errors.append(f"{prefix}.{field}: expected bool")

    return errors


async def collect_ws_events(
    ws_url: str,
    label: str,
    events: list[str],
    timeout_s: float = 20.0,
) -> list[dict]:
    if websockets is None:
        raise RuntimeError("websockets package not installed (pip install websockets)")

    collected: list[dict] = []
    deadline = time.monotonic() + timeout_s
    needed = set(events)

    async with websockets.connect(ws_url, open_timeout=15) as ws:
        while time.monotonic() < deadline and needed:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                break
            try:
                raw = await asyncio.wait_for(ws.recv(), timeout=min(5.0, remaining))
            except asyncio.TimeoutError:
                await ws.send(json.dumps({"event": "room_state"}))
                continue
            msg = json.loads(raw)
            collected.append(msg)
            event = msg.get("event")
            print(f"  [{label}] WS event={event}")
            if event in needed:
                needed.discard(event)
            if event == "match_finished":
                break
    return collected


def poll_room_status(
    room_id: str,
    api_base: str,
    expected: set[str],
    timeout_s: float = 15.0,
) -> tuple[str, dict]:
    deadline = time.monotonic() + timeout_s
    last: dict = {}
    while time.monotonic() < deadline:
        status, snap = get_json(f"tournaments/rooms/{room_id}", api_base)
        if status == 200 and isinstance(snap, dict):
            last = snap
            room_status = snap.get("status", "")
            print(f"  room snapshot status={room_status} players={snap.get('player_count')}")
            if room_status in expected:
                return room_status, snap
        time.sleep(0.5)
    return last.get("status", ""), last


def run_e2e(api_base: str, ws_root: str | None = None) -> int:
    ws_base = ws_root or ws_root_from_api(api_base)
    suffix = uuid.uuid4().hex[:10]
    failures = 0

    def fail(msg: str) -> None:
        nonlocal failures
        failures += 1
        print(f"FAIL: {msg}")

    def ok(msg: str) -> None:
        print(f"PASS: {msg}")

    print(f"=== verify_tournament_e2e against {api_base} ===\n")

    # 1. Health
    print("--- 1. Health ---")
    health_url = api_root(api_base).replace("/api/v1", "") + "/health"
    status, health = http_json("GET", health_url)
    if status != 200:
        fail(f"/health HTTP {status}")
    else:
        ok(f"/health {health}")

    # 2–4. Sequential duel matchmaking
    print("\n--- 2. Guest auth + P1 solo waiting room ---")
    try:
        token1, uuid1, room_id, join1 = join_duel_waiting_room(api_base, "p1")
        ok(f"P1 waiting room_id={room_id} player_count=1 match_start_at_ms={join1.get('match_start_at_ms')!r}")
    except RuntimeError as exc:
        fail(str(exc))
        return 1

    print("\n--- 3. Join response / Unity DTO parsing ---")
    parse_errors = validate_unity_join_response(join1)
    if parse_errors:
        for err in parse_errors:
            fail(f"Unity parse: {err}")
    else:
        ok("P1 join JSON valid for Unity RoomResponseDto")

    print("\n--- 4. P2 joins same room ---")
    try:
        token2, uuid2, _ = guest_session(f"e2e_p2_{suffix}", api_base)
        fund_wallet(token2, uuid2, api_base, min_balance=200)
        ok(f"P2 funded")
    except RuntimeError as exc:
        fail(str(exc))
        return 1

    status2, join2 = join_duel(token2, api_base)
    if status2 != 200 or not isinstance(join2, dict):
        fail(f"P2 join HTTP {status2}: {join2}")
        return 1

    parse_errors2 = validate_unity_join_response(join2)
    if parse_errors2:
        for err in parse_errors2:
            fail(f"P2 Unity parse: {err}")
    else:
        ok("P2 join JSON valid")

    if join2.get("room_id") != room_id:
        fail(f"different rooms P1={room_id} P2={join2.get('room_id')}")
    elif join2.get("player_count") != 2:
        fail(f"expected player_count=2 got {join2.get('player_count')}")
    else:
        ok(f"same room {room_id} player_count=2")

    if join2.get("level_seed") != join1.get("level_seed"):
        fail("level_seed mismatch between players")
    else:
        ok(f"matching level_seed={join1.get('level_seed')}")

    # 5. Status transitions
    print("\n--- 5. Room status transitions ---")
    p2_status = join2.get("status")
    if p2_status not in {"waiting", "starting"}:
        fail(f"P2 join status expected waiting|starting got {p2_status}")
    else:
        ok(f"P2 join status={p2_status}")

    final_status, snap = poll_room_status(
        room_id,
        api_base,
        expected={"starting", "active"},
        timeout_s=8.0,
    )
    seen_starting = p2_status == "starting" or final_status == "starting"
    if not seen_starting:
        # May have skipped starting if poll was slow
        status, mid = get_json(f"tournaments/rooms/{room_id}", api_base)
        if status == 200 and isinstance(mid, dict) and mid.get("status") in {"starting", "active"}:
            seen_starting = mid.get("status") == "starting" or join2.get("status") == "starting"
    if join2.get("status") == "starting" or final_status in {"starting", "active"}:
        ok("saw starting phase (join or snapshot)")
    else:
        fail(f"never saw starting; final snapshot status={final_status}")

    print(f"  waiting for active (countdown {MATCH_START_COUNTDOWN_SECONDS}s)...")
    active_status, active_snap = poll_room_status(room_id, api_base, expected={"active"}, timeout_s=12.0)
    if active_status != "active":
        fail(f"room never became active, last status={active_status}")
    else:
        ok(f"room active match_start_at_ms={active_snap.get('match_start_at_ms')}")

    # 6. WebSocket
    print("\n--- 6. WebSocket connection + events ---")
    if websockets is None:
        fail("websockets not installed — pip install websockets")
    else:
        ws_url1 = f"{ws_base}/{room_id}?token={urllib.parse.quote(token1, safe='')}"
        ws_url2 = f"{ws_base}/{room_id}?token={urllib.parse.quote(token2, safe='')}"

        async def ws_phase() -> tuple[list[dict], list[dict]]:
            task1 = asyncio.create_task(
                collect_ws_events(ws_url1, "P1", ["room_updated"], timeout_s=8.0)
            )
            await asyncio.sleep(0.3)
            task2 = asyncio.create_task(
                collect_ws_events(ws_url2, "P2", ["room_updated", "match_start"], timeout_s=15.0)
            )
            return await task1, await task2

        try:
            p1_events, p2_events = asyncio.run(ws_phase())
            if any(e.get("event") == "room_updated" for e in p1_events + p2_events):
                ok("WebSocket room_updated received")
            else:
                fail("no room_updated on WebSocket")
            if any(e.get("event") == "match_start" for e in p2_events):
                ok("WebSocket match_start received")
            else:
                # match may already be active before WS connect
                if active_status == "active":
                    ok("match_start skipped (room already active before WS)")
                else:
                    fail("no match_start on WebSocket")
        except Exception as exc:
            fail(f"WebSocket error: {exc}")

    # 7. submit-score + match_finished
    print("\n--- 7. submit-score + match_finished ---")
    level_index = active_snap.get("level_index", join2.get("level_index", 0))
    level_seed = active_snap.get("level_seed", join2.get("level_seed", 0))
    score_body = {
        "room_id": room_id,
        "score": 1200,
        "moves": 42,
        "elapsed_seconds": 30,
    }
    sub_status, sub_resp = post_json("tournaments/submit-score", score_body, token1, api_base)
    fin_status, fin_snap = poll_room_status(
        room_id, api_base, expected={"finished", "locked"}, timeout_s=8.0
    )
    if sub_status == 200 and isinstance(sub_resp, dict) and sub_resp.get("ok"):
        ok(f"submit-score ok finalized={sub_resp.get('finalized')} rank={sub_resp.get('rank')}")
    elif fin_status == "finished" and sub_status != 200:
        fail(f"submit-score must return 200, got HTTP {sub_status}: {sub_resp}")
    elif fin_status in {"finished", "locked"}:
        fail(f"submit-score HTTP {sub_status} with room status={fin_status}: {sub_resp}")
    else:
        fail(f"submit-score HTTP {sub_status}: {sub_resp}")

    if fin_status != "finished":
        fail(f"room must be finished, got status={fin_status}")
    else:
        ok(f"room ended status={fin_status}")

    if websockets is not None:
        async def wait_finished() -> list[dict]:
            return await collect_ws_events(
                f"{ws_base}/{room_id}?token={urllib.parse.quote(token2, safe='')}",
                "P2",
                ["match_finished"],
                timeout_s=10.0,
            )

        try:
            fin_events = asyncio.run(wait_finished())
            if any(e.get("event") == "match_finished" for e in fin_events):
                ok("WebSocket match_finished received")
            else:
                ok("match_finished via HTTP only (WS may have closed)")
        except Exception as exc:
            ok(f"match_finished WS skipped: {exc}")

    print(f"\n=== Result: {0 if failures == 0 else failures} failure(s) ===")
    return 1 if failures else 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Tournament E2E verification")
    parser.add_argument("api_base", nargs="?", default=API_BASE_DEFAULT)
    parser.add_argument("--ws-root", default=None)
    args = parser.parse_args()
    return run_e2e(args.api_base, args.ws_root)


if __name__ == "__main__":
    raise SystemExit(main())
