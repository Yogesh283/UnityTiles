#!/usr/bin/env python3
"""Verify duel_1v1 matchmaking places two players in the same room."""

from __future__ import annotations

import json
import sys
import time
import uuid
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed

API_BASE = "https://api.matchiq.fun/api/v1"


def post_json(path: str, body: dict, token: str | None = None, api_base: str = API_BASE) -> tuple[int, dict | str]:
    data = json.dumps(body).encode("utf-8")
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(f"{api_base}/{path}", data=data, headers=headers, method="POST")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            return resp.status, json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        raw = exc.read().decode("utf-8", errors="replace")
        try:
            return exc.code, json.loads(raw)
        except json.JSONDecodeError:
            return exc.code, raw


def guest_login(guest_id: str, api_base: str) -> tuple[str, str]:
    status, payload = post_json("auth/guest", {"guest_id": guest_id, "display_name": guest_id}, api_base=api_base)
    if status != 200:
        raise RuntimeError(f"guest login failed ({status}): {payload}")
    return payload["access_token"], payload["user_uuid"]


def get_json(path: str, api_base: str, token: str | None = None) -> tuple[int, dict | str]:
    headers = {}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(f"{api_base}/{path}", headers=headers, method="GET")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            return resp.status, json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        raw = exc.read().decode("utf-8", errors="replace")
        try:
            return exc.code, json.loads(raw)
        except json.JSONDecodeError:
            return exc.code, raw


def fund_wallet(token: str, user_uuid: str, api_base: str, min_balance: int = 200) -> None:
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


def join_duel(token: str, api_base: str) -> tuple[int, dict | str]:
    for attempt in range(1, 9):
        status, body = post_json("tournaments/join", {"tournament_id": "duel_1v1"}, token, api_base=api_base)
        if status == 200:
            return status, body
        if (
            status == 400
            and isinstance(body, dict)
            and "matchmaking busy" in str(body.get("detail", "")).lower()
            and attempt < 8
        ):
            time.sleep(min(2.0 * attempt, 8.0))
            continue
        return status, body
    return status, body


def join_solo_waiting_room(api_base: str, label: str) -> tuple[str, str, dict]:
    """Join as first player in a fresh waiting room (player_count=1)."""
    for attempt in range(5):
        guest_id = f"duel_{label}_{uuid.uuid4().hex[:10]}"
        token, user_uuid = guest_login(guest_id, api_base)
        fund_wallet(token, user_uuid, api_base)
        status, join = join_duel(token, api_base)
        if status != 200 or not isinstance(join, dict):
            raise RuntimeError(f"{label} join failed ({status}): {join}")
        if join.get("status") == "waiting" and join.get("player_count") == 1:
            return token, user_uuid, join
        print(
            f"  {label} solo-waiting attempt {attempt + 1}: "
            f"status={join.get('status')} players={join.get('player_count')} — retrying"
        )
        time.sleep(0.5)
    raise RuntimeError(f"{label} could not create solo waiting room")


def verify_sequential(api_base: str, suffix: str) -> int:
    try:
        token1, _, join1 = join_solo_waiting_room(api_base, f"verify_p1_{suffix}")
    except RuntimeError as exc:
        print(f"FAIL {exc}")
        return 1

    room1 = join1["room_id"]
    seed1 = join1["level_seed"]
    print(
        f"PASS P1 join room_id={room1} seed={seed1} status={join1.get('status')} "
        f"players={join1.get('player_count')} match_start_at_ms={join1.get('match_start_at_ms')!r}"
    )

    token2, uuid2 = guest_login(f"duel_verify_p2_{suffix}", api_base)
    fund_wallet(token2, uuid2, api_base)
    status2, join2 = join_duel(token2, api_base)
    if status2 != 200:
        print(f"FAIL P2 join ({status2}): {join2}")
        return 1

    room2 = join2["room_id"]
    seed2 = join2["level_seed"]
    print(
        f"PASS P2 join room_id={room2} seed={seed2} status={join2.get('status')} "
        f"players={join2.get('player_count')} countdown={join2.get('start_countdown_seconds')} "
        f"match_start_at_ms={join2.get('match_start_at_ms')}"
    )

    if room1 != room2:
        print(f"FAIL different rooms: P1={room1} P2={room2}")
        return 1

    if seed1 != seed2:
        print(f"FAIL different seeds: P1={seed1} P2={seed2}")
        return 1

    if join2.get("player_count") != 2:
        print(f"FAIL expected player_count=2 got {join2.get('player_count')}")
        return 1

    print(f"PASS same room {room1} with 2 players and matching seed")
    return 0


def verify_concurrent(api_base: str, suffix: str) -> int:
    print("--- concurrent join test (2 threads) ---")
    token1, uuid1 = guest_login(f"duel_race_p1_{suffix}", api_base)
    fund_wallet(token1, uuid1, api_base)
    token2, uuid2 = guest_login(f"duel_race_p2_{suffix}", api_base)
    fund_wallet(token2, uuid2, api_base)

    results: list[tuple[str, int, dict | str]] = []

    def worker(label: str, token: str) -> tuple[str, int, dict | str]:
        return label, *join_duel(token, api_base)

    with ThreadPoolExecutor(max_workers=2) as pool:
        futures = [
            pool.submit(worker, "P1", token1),
            pool.submit(worker, "P2", token2),
        ]
        for future in as_completed(futures):
            label, status, body = future.result()
            results.append((label, status, body))
            print(f"{label} join status={status} body={body}")

    if any(status != 200 for _, status, _ in results):
        print("FAIL concurrent join HTTP error")
        return 1

    rooms = {body["room_id"] for _, _, body in results if isinstance(body, dict)}
    seeds = {body["level_seed"] for _, _, body in results if isinstance(body, dict)}
    counts = [body.get("player_count") for _, _, body in results if isinstance(body, dict)]

    if len(rooms) != 1:
        print(f"FAIL concurrent different rooms: {rooms}")
        return 1

    if len(seeds) != 1:
        print(f"FAIL concurrent different seeds: {seeds}")
        return 1

    if max(counts) != 2:
        print(f"FAIL concurrent expected final player_count=2 got {counts}")
        return 1

    print(f"PASS concurrent same room {rooms.pop()} player_count=2")
    return 0


def main() -> int:
    api_base = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else API_BASE
    print(f"verify_duel_matchmaking against {api_base}")

    suffix = str(int(time.time()))
    code = verify_sequential(api_base, suffix)
    if code != 0:
        return code

    return verify_concurrent(api_base, suffix + "_race")


if __name__ == "__main__":
    raise SystemExit(main())
