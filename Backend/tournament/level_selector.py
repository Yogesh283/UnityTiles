import random

from tournament.catalog import TournamentDefinition


def _csharp_string_hash(value: str) -> int:
    """Portable approximation of Unity/C# string hash for room seeds."""
    h = 0
    for ch in value:
        h = (h * 31 + ord(ch)) & 0xFFFFFFFF
    if h >= 0x80000000:
        h -= 0x100000000
    return h


def _to_int32(value: int) -> int:
    """Match C# unchecked 32-bit integer overflow for cross-platform room seeds."""
    value &= 0xFFFFFFFF
    if value >= 0x80000000:
        value -= 0x100000000
    return value


def generate_room_seed(tournament_id: str, room_id: str) -> int:
    a = _csharp_string_hash(tournament_id)
    b = _csharp_string_hash(room_id)
    return _to_int32(_to_int32(a * 397) ^ b)


def pick_level_index(room_seed: int, tournament: TournamentDefinition, level_count: int = 100) -> int:
    count = max(level_count, 1)
    rng = random.Random(room_seed ^ _csharp_string_hash(tournament.id))
    return rng.randint(0, count - 1)
