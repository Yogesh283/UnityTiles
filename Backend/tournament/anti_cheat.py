from __future__ import annotations

from dataclasses import dataclass

from database.models import RoomPlayer, TournamentRoom


@dataclass
class ScoreSubmission:
    score: int
    moves: int
    elapsed_seconds: int
    level_index: int
    level_seed: int


class AntiCheatError(Exception):
    pass


MIN_ELAPSED_SECONDS = 3
MAX_ELAPSED_SECONDS = 7200
MIN_MOVES = 1
MAX_MOVES = 50000
MAX_SCORE = 5_000_000


def validate_score_submission(
    room: TournamentRoom,
    player: RoomPlayer,
    submission: ScoreSubmission,
) -> None:
    if room.status not in {"starting", "active", "waiting"}:
        raise AntiCheatError("Room is not accepting scores")

    if player.finished_at is not None:
        raise AntiCheatError("Score already submitted")

    if submission.level_index != room.level_index:
        raise AntiCheatError("Level mismatch")

    if submission.level_seed != room.level_seed:
        raise AntiCheatError("Seed mismatch")

    if submission.elapsed_seconds < MIN_ELAPSED_SECONDS:
        raise AntiCheatError("Completion time too fast")

    if submission.elapsed_seconds > MAX_ELAPSED_SECONDS:
        raise AntiCheatError("Completion time too slow")

    if submission.moves < MIN_MOVES:
        raise AntiCheatError("Invalid move count")

    if submission.moves > MAX_MOVES:
        raise AntiCheatError("Move count too high")

    if submission.score < 0 or submission.score > MAX_SCORE:
        raise AntiCheatError("Invalid score")

    if submission.moves > 0 and submission.score > submission.moves * 100_000:
        raise AntiCheatError("Score inconsistent with moves")
