"""Read deployed git revision for /health verification (not tournament logic)."""

from __future__ import annotations

import subprocess
from functools import lru_cache
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]


@lru_cache(maxsize=1)
def deployed_git_info() -> dict[str, str | None]:
    branch: str | None = None
    commit: str | None = None
    subject: str | None = None
    try:
        branch = (
            subprocess.check_output(
                ["git", "rev-parse", "--abbrev-ref", "HEAD"],
                cwd=REPO_ROOT,
                text=True,
                stderr=subprocess.DEVNULL,
            )
            .strip()
            or None
        )
        commit = (
            subprocess.check_output(
                ["git", "rev-parse", "--short", "HEAD"],
                cwd=REPO_ROOT,
                text=True,
                stderr=subprocess.DEVNULL,
            )
            .strip()
            or None
        )
        subject = (
            subprocess.check_output(
                ["git", "log", "-1", "--pretty=%s"],
                cwd=REPO_ROOT,
                text=True,
                stderr=subprocess.DEVNULL,
            )
            .strip()
            or None
        )
    except (subprocess.CalledProcessError, FileNotFoundError, OSError):
        pass
    return {"branch": branch, "commit": commit, "subject": subject}
