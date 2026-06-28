from sqlalchemy.orm import Session

from database.models import DeviceBan, IpBan, User


class SecurityService:
    def __init__(self, db: Session):
        self.db = db

    def is_ip_banned(self, ip_address: str | None) -> bool:
        if not ip_address:
            return False
        return (
            self.db.query(IpBan)
            .filter(IpBan.ip_address == ip_address, IpBan.is_active.is_(True))
            .first()
            is not None
        )

    def is_device_banned(self, device_id: str | None) -> bool:
        if not device_id:
            return False
        return (
            self.db.query(DeviceBan)
            .filter(DeviceBan.device_id == device_id, DeviceBan.is_active.is_(True))
            .first()
            is not None
        )

    def assert_user_allowed(self, user: User, ip_address: str | None, device_id: str | None) -> None:
        if not user.is_active or user.is_banned:
            raise PermissionError("Account is banned or inactive")
        if self.is_ip_banned(ip_address):
            raise PermissionError("IP address is banned")
        if self.is_device_banned(device_id):
            raise PermissionError("Device is banned")

    def touch_user_session(self, user: User, ip_address: str | None, device_id: str | None) -> None:
        if ip_address:
            user.last_ip = ip_address
        if device_id:
            user.last_device_id = device_id
        self.db.commit()
