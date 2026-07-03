#!/usr/bin/env python3
"""
Load-test duel_1v1 matchmaking with N dummy guest users.

Usage:
  python Backend/scripts/load_test_duel_users.py
  python Backend/scripts/load_test_duel_users.py --users 1000 --workers 40
  python Backend/scripts/load_test_duel_users.py --api http://localhost:8000/api/v1 --users 50
"""

from __future__ import annotations

import argparse
import json
import statistics
import sys
import time
import urllib.error
import urllib.request
import uuid
from collections import Counter, defaultdict
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import dataclass, field
from typing import Any

API_BASE_DEFAULT = "https://api.matchiq.fun/api/v1"
DUEL_ENTRY_FEE = 100
FUND_LEVELS = (1, 2)  # 50 coins each → 100 total


def post_json(path: str, body: dict, token: str | None, api_base: str, timeout: int = 60) -> tuple[int, Any]:
    data = json.dumps(body).encode("utf-8")
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(f"{api_base.rstrip('/')}/{path.lstrip('/')}", data=data, headers=headers, method="POST")
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
    except urllib.error.URLError as exc:
        return 0, str(exc)


def get_json(path: str, api_base: str, token: str | None = None, timeout: int = 60) -> tuple[int, Any]:
    headers = {}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    req = urllib.request.Request(f"{api_base.rstrip('/')}/{path.lstrip('/')}", headers=headers, method="GET")
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
    except urllib.error.URLError as exc:
        return 0, str(exc)


def guest_login(guest_id: str, api_base: str) -> tuple[str, str]:
    last_error = ""
    for attempt in range(1, 6):
        status, payload = post_json("auth/guest", {"guest_id": guest_id, "display_name": guest_id}, None, api_base)
        if status == 200 and isinstance(payload, dict):
            return payload["access_token"], payload["user_uuid"]
        last_error = f"guest login failed ({status}): {payload}"
        if status in (0, 500, 502, 503, 504) or "timed out" in str(payload).lower():
            time.sleep(min(2.0 * attempt, 10.0))
            continue
        break
    raise RuntimeError(last_error)


def fund_wallet(token: str, user_uuid: str, api_base: str) -> int:
    balance = 0
    for level in FUND_LEVELS:
        status, resp = post_json(
            "levels/complete",
            {"user_uuid": user_uuid, "level_number": level},
            token,
            api_base,
        )
        if status != 200 or not isinstance(resp, dict):
            raise RuntimeError(f"fund level {level} failed ({status}): {resp}")
        balance = int(resp.get("current_wallet_balance", balance))
    if balance < DUEL_ENTRY_FEE:
        raise RuntimeError(f"wallet underfunded: {balance} < {DUEL_ENTRY_FEE}")
    return balance


def join_duel(token: str, api_base: str) -> tuple[int, Any, float]:
    started = time.perf_counter()
    last_status, last_body = 0, None
    for attempt in range(1, 10):
        status, body = post_json("tournaments/join", {"tournament_id": "duel_1v1"}, token, api_base)
        last_status, last_body = status, body
        if status == 200:
            return status, body, time.perf_counter() - started
        if (
            status == 400
            and isinstance(body, dict)
            and "matchmaking busy" in str(body.get("detail", "")).lower()
            and attempt < 9
        ):
            time.sleep(min(1.5 * attempt, 10.0))
            continue
        break
    return last_status, last_body, time.perf_counter() - started


@dataclass
class UserResult:
    index: int
    ok: bool
    error: str = ""
    room_id: str | None = None
    player_count: int = 0
    status: str = ""
    level_seed: int | None = None
    match_start_at_ms: int | None = None
    start_countdown_seconds: int | None = None
    join_latency_s: float = 0.0
    guest_id: str = ""


@dataclass
class LoadTestReport:
    users_requested: int
    users_ok: int = 0
    users_failed: int = 0
    errors: Counter = field(default_factory=Counter)
    join_latencies: list[float] = field(default_factory=list)
    room_members: dict[str, list[int]] = field(default_factory=lambda: defaultdict(list))
    room_snapshots: dict[str, dict] = field(default_factory=dict)
    status_counts: Counter = field(default_factory=Counter)
    paired_rooms: int = 0
    paired_with_match_start: int = 0
    paired_active_or_starting: int = 0
    orphan_waiting_rooms: int = 0
    sample_failures: list[str] = field(default_factory=list)


def run_user(index: int, run_id: str, api_base: str) -> UserResult:
    guest_id = f"load_{run_id}_{index:04d}_{uuid.uuid4().hex[:6]}"
    try:
        token, user_uuid = guest_login(guest_id, api_base)
        fund_wallet(token, user_uuid, api_base)
        status, body, latency = join_duel(token, api_base)
        if status != 200 or not isinstance(body, dict):
            detail = body.get("detail", body) if isinstance(body, dict) else body
            return UserResult(index=index, ok=False, error=f"join HTTP {status}: {detail}", guest_id=guest_id)
        return UserResult(
            index=index,
            ok=True,
            room_id=body.get("room_id"),
            player_count=int(body.get("player_count") or 0),
            status=str(body.get("status") or ""),
            level_seed=body.get("level_seed"),
            match_start_at_ms=body.get("match_start_at_ms"),
            start_countdown_seconds=body.get("start_countdown_seconds"),
            join_latency_s=latency,
            guest_id=guest_id,
        )
    except Exception as exc:  # noqa: BLE001 — aggregate per-user errors in load test
        return UserResult(index=index, ok=False, error=str(exc), guest_id=guest_id)


def analyze(results: list[UserResult], api_base: str, sample_rooms: int) -> LoadTestReport:
    report = LoadTestReport(users_requested=len(results))
    for result in results:
        if result.ok:
            report.users_ok += 1
            report.join_latencies.append(result.join_latency_s)
            report.status_counts[result.status] += 1
            if result.room_id:
                report.room_members[result.room_id].append(result.index)
        else:
            report.users_failed += 1
            report.errors[result.error.split(":")[0][:80]] += 1
            if len(report.sample_failures) < 8:
                report.sample_failures.append(f"user {result.index}: {result.error}")

    for room_id, members in report.room_members.items():
        if len(members) == 2:
            report.paired_rooms += 1
        elif len(members) == 1:
            report.orphan_waiting_rooms += 1

    # Snapshot a sample of paired rooms from API (server truth)
    paired_ids = [rid for rid, mem in report.room_members.items() if len(mem) == 2]
    for room_id in paired_ids[:sample_rooms]:
        status, snap = get_json(f"tournaments/rooms/{room_id}", api_base)
        if status == 200 and isinstance(snap, dict):
            report.room_snapshots[room_id] = snap
            ms = snap.get("match_start_at_ms") or 0
            st = str(snap.get("status") or "")
            if ms > 0:
                report.paired_with_match_start += 1
            if st in {"starting", "active", "locked"}:
                report.paired_active_or_starting += 1

    return report


def print_report(report: LoadTestReport, elapsed_s: float, api_base: str, sample_rooms: int) -> int:
    print()
    print("=" * 72)
    print(f"LOAD TEST REPORT — {api_base}")
    print("=" * 72)
    print(f"Users requested : {report.users_requested}")
    print(f"Users joined OK : {report.users_ok}")
    print(f"Users failed    : {report.users_failed}")
    print(f"Elapsed         : {elapsed_s:.1f}s")
    if report.join_latencies:
        print(
            f"Join latency    : p50={statistics.median(report.join_latencies):.2f}s "
            f"p95={sorted(report.join_latencies)[int(len(report.join_latencies) * 0.95) - 1]:.2f}s "
            f"max={max(report.join_latencies):.2f}s"
        )
    print()
    print("Room pairing (client join responses):")
    print(f"  Unique rooms       : {len(report.room_members)}")
    print(f"  Paired (2 players) : {report.paired_rooms}")
    print(f"  Solo waiting (1)   : {report.orphan_waiting_rooms}")
    print(f"  Status breakdown   : {dict(report.status_counts)}")
    print()
    expected_pairs = report.users_ok // 2
    pair_ok = report.paired_rooms >= expected_pairs * 0.95
    print(f"Freeze-relevant checks (sampled {min(sample_rooms, report.paired_rooms)} paired rooms):")
    print(f"  match_start_at_ms set : {report.paired_with_match_start}/{min(sample_rooms, report.paired_rooms)}")
    print(f"  status starting/active: {report.paired_active_or_starting}/{min(sample_rooms, report.paired_rooms)}")
    print()

    if report.sample_failures:
        print("Sample failures:")
        for line in report.sample_failures:
            print(f"  - {line}")
        print()

    if report.errors:
        print("Error types:")
        for err, count in report.errors.most_common(10):
            print(f"  {count:4d} × {err}")
        print()

    # Show 3 example paired room snapshots
    shown = 0
    for room_id, snap in report.room_snapshots.items():
        if shown >= 3:
            break
        print(
            f"Example room {room_id}: status={snap.get('status')} "
            f"players={snap.get('player_count')} match_start_at_ms={snap.get('match_start_at_ms')}"
        )
        shown += 1

    passed = (
        report.users_failed == 0
        and report.paired_rooms >= expected_pairs * 0.95
        and report.orphan_waiting_rooms <= max(2, report.users_ok * 0.02)
        and (
            report.paired_rooms == 0
            or report.paired_with_match_start >= min(sample_rooms, report.paired_rooms) * 0.9
        )
    )
    print()
    print("VERDICT:", "PASS" if passed else "FAIL")
    print("=" * 72)
    return 0 if passed else 1


def main() -> int:
    parser = argparse.ArgumentParser(description="Load-test duel_1v1 with dummy guest users")
    parser.add_argument("--api", default=API_BASE_DEFAULT, help="API base URL")
    parser.add_argument("--users", type=int, default=1000, help="Number of dummy users")
    parser.add_argument("--workers", type=int, default=10, help="Concurrent threads (keep low on live)")
    parser.add_argument("--batch-size", type=int, default=50, help="Users per batch with pause between")
    parser.add_argument("--batch-pause", type=float, default=2.0, help="Seconds between batches")
    parser.add_argument("--sample-rooms", type=int, default=25, help="Paired rooms to snapshot-verify")
    args = parser.parse_args()

    api_base = args.api.rstrip("/")
    run_id = str(int(time.time()))

    print(f"load_test_duel_users: {args.users} users, {args.workers} workers")
    print(f"API: {api_base}")
    print(f"run_id: {run_id}")
    print()

    # Health check
    health_url = api_base.replace("/api/v1", "") + "/health"
    try:
        req = urllib.request.Request(health_url, method="GET")
        with urllib.request.urlopen(req, timeout=20) as resp:
            health = json.loads(resp.read().decode("utf-8"))
            print(f"Health OK: {health.get('status')} commit={health.get('commit')}")
    except Exception as exc:
        print(f"WARN: health check failed: {exc}")

    started = time.perf_counter()
    results: list[UserResult] = []

    for batch_start in range(0, args.users, args.batch_size):
        batch_end = min(batch_start + args.batch_size, args.users)
        batch_indices = range(batch_start, batch_end)
        print(f"--- batch {batch_start // args.batch_size + 1}: users {batch_start + 1}-{batch_end} ---")

        with ThreadPoolExecutor(max_workers=args.workers) as pool:
            futures = [pool.submit(run_user, i, run_id, api_base) for i in batch_indices]
            for future in as_completed(futures):
                results.append(future.result())

        ok = sum(1 for r in results if r.ok)
        print(f"  cumulative {len(results)}/{args.users} (ok={ok}, fail={len(results) - ok})")
        if batch_end < args.users and args.batch_pause > 0:
            time.sleep(args.batch_pause)

    elapsed = time.perf_counter() - started
    report = analyze(results, api_base, args.sample_rooms)
    return print_report(report, elapsed, api_base, args.sample_rooms)


if __name__ == "__main__":
    raise SystemExit(main())
