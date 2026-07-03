from __future__ import annotations

import json
from typing import Any

import redis

from config import get_settings

_client: redis.Redis | None = None


def _reset_client() -> None:
    global _client
    _client = None


def get_redis() -> redis.Redis | None:
    global _client
    if _client is not None:
        try:
            _client.ping()
            return _client
        except Exception:
            _reset_client()
    try:
        _client = redis.from_url(get_settings().redis_url, decode_responses=True)
        _client.ping()
        return _client
    except Exception:
        _reset_client()
        return None


def cache_get(key: str) -> Any | None:
    client = get_redis()
    if not client:
        return None
    try:
        raw = client.get(key)
    except Exception:
        _reset_client()
        return None
    if raw is None:
        return None
    try:
        return json.loads(raw)
    except json.JSONDecodeError:
        return raw


def cache_set(key: str, value: Any, ttl_seconds: int = 60) -> None:
    client = get_redis()
    if not client:
        return
    payload = value if isinstance(value, str) else json.dumps(value)
    try:
        client.setex(key, ttl_seconds, payload)
    except Exception:
        _reset_client()


def rate_limit_check(key: str, limit: int, window_seconds: int) -> bool:
    """Return True if request is allowed."""
    client = get_redis()
    if not client:
        return True
    try:
        pipe = client.pipeline()
        pipe.incr(key)
        pipe.expire(key, window_seconds)
        count, _ = pipe.execute()
        return int(count) <= limit
    except Exception:
        _reset_client()
        return True


def set_online_user(user_uuid: str, ttl_seconds: int = 120) -> None:
    client = get_redis()
    if not client:
        return
    try:
        client.setex(f"online:user:{user_uuid}", ttl_seconds, "1")
    except Exception:
        _reset_client()


def count_online_users() -> int:
    client = get_redis()
    if not client:
        return 0
    try:
        return len(client.keys("online:user:*"))
    except Exception:
        _reset_client()
        return 0
