#!/usr/bin/env python3
"""Smoke-test join + room snapshot for every tournament in the catalog."""

from __future__ import annotations

import json
import sys
import time
import urllib.error
import urllib.request

API_BASE = "https://api.matchiq.fun/api/v1"

TOURNAMENT_IDS = [
    "duel_1v1",
    "quick_cup",
    "mega_clash",
    "grand_clash",
    "championship",
    "world_cup",
]


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


def get_json(path: str, api_base: str = API_BASE) -> tuple[int, dict | str]:
    req = urllib.request.Request(f"{api_base}/{path}", method="GET")
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
    status, payload = post_json("auth/guest", {"guest_id": guest_id, "display_name": "FreezeTest"}, api_base=api_base)
    if status != 200:
        raise RuntimeError(f"guest login failed ({status}): {payload}")
    return payload["access_token"], payload["user_uuid"]


TOURNAMENT_ENTRY_FEES = {
    "duel_1v1": 100,
    "quick_cup": 100,
    "mega_clash": 200,
    "grand_clash": 500,
    "championship": 1000,
    "world_cup": 2000,
}


def fund_wallet(token: str, user_uuid: str, api_base: str, min_balance: int = 500) -> None:
    level = 1
    balance = 0
    while balance < min_balance and level <= 40:
        status, resp = post_json(
            "levels/complete",
            {"user_uuid": user_uuid, "level_number": level},
            token,
            api_base=api_base,
        )
        if status != 200:
            raise RuntimeError(f"level complete failed ({status}): {resp}")
        balance = resp.get("current_wallet_balance", balance)
        level += 1
    if balance < min_balance:
        raise RuntimeError(f"could not fund wallet to {min_balance}, got {balance}")


def join_tournament(token: str, tournament_id: str, api_base: str) -> tuple[int, dict | str]:
    for attempt in range(1, 9):
        status, body = post_json(
            "tournaments/join", {"tournament_id": tournament_id}, token, api_base=api_base
        )
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


def main() -> int:
    api_base = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else API_BASE

    print(f"Testing tournaments against {api_base}")
    failures = 0

    for index, tournament_id in enumerate(TOURNAMENT_IDS):
        token, user_uuid = guest_login(f"freeze_test_{tournament_id}_{index}", api_base)
        entry_fee = TOURNAMENT_ENTRY_FEES.get(tournament_id, 500)
        fund_wallet(token, user_uuid, api_base, min_balance=entry_fee + 200)
        status, join = join_tournament(token, tournament_id, api_base)
        ok = status == 200 and isinstance(join, dict)
        room_id = join.get("room_id") if ok else None
        level_seed = join.get("level_seed") if ok else None

        snap_status, snap = get_json(f"tournaments/rooms/{room_id}", api_base=api_base) if room_id else (0, None)
        snap_ok = snap_status == 200 and isinstance(snap, dict)

        seed_ok = isinstance(level_seed, int) and -2_147_483_648 <= level_seed <= 2_147_483_647
        player_fields_ok = True
        has_submitted_field = True
        if snap_ok and snap.get("players"):
            for player in snap["players"]:
                if "has_submitted" not in player:
                    has_submitted_field = False

        passed = ok and snap_ok and seed_ok
        mark = "PASS" if passed else "FAIL"
        deploy_note = "" if has_submitted_field else " [deploy API for duel freeze fix]"
        print(
            f"[{mark}] {tournament_id}: join={status} room={room_id} "
            f"seed={level_seed} snapshot={snap_status} players={len(snap.get('players', [])) if snap_ok else '?'}"
            f"{deploy_note}"
        )
        if not passed:
            failures += 1
            print(f"       join_body={join}")
            if not snap_ok:
                print(f"       snap_body={snap}")

    print(f"\n{len(TOURNAMENT_IDS) - failures}/{len(TOURNAMENT_IDS)} passed")
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
