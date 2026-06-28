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


def generate_room_seed(tournament_id: str, room_id: str) -> int:
    return _csharp_string_hash(tournament_id) * 397 ^ _csharp_string_hash(room_id)


def pick_level_index(room_seed: int, tournament: TournamentDefinition, level_count: int = 100) -> int:
    count = max(level_count, 1)
    rng = random.Random(room_seed ^ _csharp_string_hash(tournament.id))
    return rng.randint(0, count - 1)
