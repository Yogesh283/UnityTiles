"""IQFX Pro Pool Play reward rules.

Mirrors the client-side rules in MatchIQ_App/src/constants/poolRules.ts so the app and
backend agree on winners count, prize distribution and prize pool math.

    Prize Pool = 70% of total collection.
    Remaining 30% = platform fee (gateway + operations + promotions).
"""

from __future__ import annotations

import json

PRIZE_SHARE = 0.70
PLATFORM_FEE_SHARE = 0.30
ENTRY_FEES = (10, 50, 100)
PLAYER_SIZES = (10, 50, 100, 500, 1000)


def winners_for(players: int) -> int:
    """How many winners get paid for a pool of the given size."""
    if players <= 50:
        return 1
    if players <= 100:
        return 2
    return 3


def distribution_percents(winners: int) -> list[int]:
    """Winner share percentages (index 0 = 1st place)."""
    if winners <= 1:
        return [100]
    if winners == 2:
        return [70, 30]
    if winners == 3:
        return [60, 25, 15]
    # Generic fallback for larger winner counts: 1st gets the biggest slice, the rest
    # split the remainder evenly. Kept deterministic and summing to 100.
    head = 50
    rest = winners - 1
    each = (100 - head) // rest
    percents = [head] + [each] * rest
    percents[0] += 100 - sum(percents)
    return percents


def prize_pool(players: int, entry_fee: int, share: float = PRIZE_SHARE) -> int:
    """70% of the total collection, floored to a whole rupee."""
    return int(players * entry_fee * share)


def parse_distribution(raw: str | list[int] | None, winners_count: int) -> list[int]:
    """Normalize a stored distribution (JSON string or list) to a list of ints."""
    if isinstance(raw, list):
        percents = [int(p) for p in raw]
    elif isinstance(raw, str) and raw.strip():
        try:
            parsed = json.loads(raw)
            percents = [int(p) for p in parsed] if isinstance(parsed, list) else []
        except (ValueError, TypeError):
            percents = []
    else:
        percents = []

    if not percents:
        percents = distribution_percents(winners_count)
    return percents


def split_prize(total_prize: int, distribution: list[int]) -> list[int]:
    """Split the prize pool across winners by percentage.

    Any rounding remainder is added to the 1st place so the sum always equals total_prize.
    """
    if total_prize <= 0 or not distribution:
        return []
    amounts = [total_prize * pct // 100 for pct in distribution]
    remainder = total_prize - sum(amounts)
    if amounts:
        amounts[0] += remainder
    return amounts
