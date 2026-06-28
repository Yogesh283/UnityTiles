from dataclasses import dataclass
from datetime import datetime


@dataclass
class RankedPlayer:
    user_id: int
    display_name: str
    score: int
    elapsed_seconds: int
    moves: int
    submitted_at: datetime | None = None


def rank_players(players: list[RankedPlayer]) -> list[tuple[RankedPlayer, int]]:
    """
    Production ranking (server-only):
    1. Fastest completion time
    2. Highest score
    3. Lowest moves
    4. Earliest server submission timestamp
    """
    epoch = datetime(1970, 1, 1)

    def sort_key(p: RankedPlayer) -> tuple:
        submitted = p.submitted_at or epoch
        # Non-finishers (0 score and 0 time) rank last
        finished = p.elapsed_seconds > 0 or p.score > 0
        return (
            0 if finished else 1,
            p.elapsed_seconds if finished else 999_999,
            -p.score,
            p.moves,
            submitted,
            p.user_id,
        )

    sorted_players = sorted(players, key=sort_key)
    return [(player, index + 1) for index, player in enumerate(sorted_players)]
