from __future__ import annotations

import json
from typing import Any

from sqlalchemy.orm import Session

from database.models import AuditLog, Log, SecurityEvent


def write_audit_log(
    db: Session,
    action: str,
    message: str,
    *,
    actor_type: str = "system",
    actor_id: str | None = None,
    target_type: str | None = None,
    target_id: str | None = None,
    context: dict[str, Any] | None = None,
    ip_address: str | None = None,
) -> None:
    db.add(
        AuditLog(
            actor_type=actor_type,
            actor_id=actor_id,
            action=action,
            target_type=target_type,
            target_id=target_id,
            message=message,
            context=json.dumps(context) if context else None,
            ip_address=ip_address,
        )
    )


def write_security_event(
    db: Session,
    event_type: str,
    message: str,
    *,
    user_id: int | None = None,
    severity: str = "warning",
    context: dict[str, Any] | None = None,
    ip_address: str | None = None,
    device_id: str | None = None,
) -> None:
    db.add(
        SecurityEvent(
            user_id=user_id,
            event_type=event_type,
            severity=severity,
            message=message,
            context=json.dumps(context) if context else None,
            ip_address=ip_address,
            device_id=device_id,
        )
    )
    db.add(
        Log(
            level="warning" if severity != "info" else "info",
            source="security",
            message=message,
            context=json.dumps(context) if context else None,
        )
    )
