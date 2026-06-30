#!/usr/bin/env python3
"""Verify duel_1v1 matchmaking places two players in the same room."""

from __future__ import annotations

import json
import sys
import time
import urllib.error
import urllib.request

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


def guest_login(guest_id: str, api_base: str) -> str:
    status, payload = post_json("auth/guest", {"guest_id": guest_id, "display_name": guest_id}, api_base=api_base)
    if status != 200:
        raise RuntimeError(f"guest login failed ({status}): {payload}")
    return payload["access_token"]


def main() -> int:
    api_base = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else API_BASE
    print(f"verify_duel_matchmaking against {api_base}")

    suffix = str(int(time.time()))
    token1 = guest_login(f"duel_verify_p1_{suffix}", api_base)
    status1, join1 = post_json("tournaments/join", {"tournament_id": "duel_1v1"}, token1, api_base=api_base)
    if status1 != 200:
        print(f"FAIL P1 join ({status1}): {join1}")
        return 1

    room1 = join1["room_id"]
    seed1 = join1["level_seed"]
    print(f"PASS P1 join room_id={room1} seed={seed1} status={join1.get('status')}")

    token2 = guest_login(f"duel_verify_p2_{suffix}", api_base)
    status2, join2 = post_json("tournaments/join", {"tournament_id": "duel_1v1"}, token2, api_base=api_base)
    if status2 != 200:
        print(f"FAIL P2 join ({status2}): {join2}")
        return 1

    room2 = join2["room_id"]
    seed2 = join2["level_seed"]
    print(f"PASS P2 join room_id={room2} seed={seed2} status={join2.get('status')} players={join2.get('player_count')}")

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


if __name__ == "__main__":
    raise SystemExit(main())
