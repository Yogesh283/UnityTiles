import hashlib

from sqlalchemy.orm import Session

from core.audit import write_audit_log
from core.identifiers import new_transaction_id
from database.models import Wallet, WalletTransaction


class WalletService:
    def __init__(self, db: Session):
        self.db = db

    def get_balance(self, user_id: int) -> int:
        wallet = self.db.query(Wallet).filter(Wallet.user_id == user_id).first()
        return wallet.balance if wallet else 0

    def ensure_wallet(self, user_id: int) -> Wallet:
        wallet = self.db.query(Wallet).filter(Wallet.user_id == user_id).first()
        if wallet:
            return wallet
        wallet = Wallet(user_id=user_id, balance=0)
        self.db.add(wallet)
        self.db.flush()
        return wallet

    def _apply(
        self,
        user_id: int,
        amount: int,
        tx_type: str,
        reference_id: str | None,
        reason: str,
        *,
        idempotency_key: str | None = None,
    ) -> Wallet:
        if idempotency_key:
            existing = (
                self.db.query(WalletTransaction)
                .filter(WalletTransaction.transaction_id == idempotency_key)
                .first()
            )
            if existing:
                wallet = self.db.query(Wallet).filter(Wallet.user_id == user_id).first()
                if not wallet:
                    raise ValueError("Wallet not found")
                return wallet

        wallet = self.db.query(Wallet).filter(Wallet.user_id == user_id).with_for_update().first()
        if not wallet:
            raise ValueError("Wallet not found")

        balance_before = wallet.balance
        new_balance = balance_before + amount
        if new_balance < 0:
            raise ValueError("Insufficient balance")

        wallet.balance = new_balance
        tx_id = idempotency_key or new_transaction_id()
        self.db.add(
            WalletTransaction(
                transaction_id=tx_id,
                user_id=user_id,
                amount=amount,
                balance_before=balance_before,
                balance_after=new_balance,
                type=tx_type,
                reference_id=reference_id,
                note=reason,
                reason=reason,
            )
        )
        write_audit_log(
            self.db,
            action="wallet_change",
            message=f"{tx_type}: {amount} coins",
            actor_type="user",
            actor_id=str(user_id),
            target_type="wallet",
            target_id=str(user_id),
            context={
                "amount": amount,
                "balance_before": balance_before,
                "balance_after": new_balance,
                "type": tx_type,
                "reference_id": reference_id,
            },
        )
        self.db.commit()
        self.db.refresh(wallet)
        return wallet

    def deduct_entry_fee(self, user_id: int, amount: int, room_id: str) -> Wallet:
        return self._apply(
            user_id,
            -amount,
            "tournament_entry",
            room_id,
            f"Tournament entry fee for room {room_id}",
            idempotency_key=f"entry:{room_id}:{user_id}",
        )

    def refund_entry_fee(self, user_id: int, amount: int, reference_id: str) -> Wallet:
        return self._apply(
            user_id,
            amount,
            "tournament_entry_refund",
            reference_id,
            f"Refund tournament entry ({reference_id})",
            idempotency_key=f"refund:{reference_id}",
        )

    def credit_prize(self, user_id: int, amount: int, room_id: str, rank: int) -> Wallet:
        idempotency_key = hashlib.sha256(f"prize:{room_id}:{user_id}".encode()).hexdigest()[:36]
        return self._apply(
            user_id,
            amount,
            "tournament_prize",
            room_id,
            f"Tournament prize rank {rank} in room {room_id}",
            idempotency_key=idempotency_key,
        )

    def admin_adjust(self, user_id: int, amount: int, reason: str) -> Wallet:
        return self._apply(user_id, amount, "admin_adjust", None, reason)

    def credit_iap(self, user_id: int, amount: int, order_id: str, product_id: str) -> Wallet:
        return self._apply(
            user_id,
            amount,
            "iap_purchase",
            order_id,
            f"Google Play purchase: {product_id}",
            idempotency_key=f"iap:{order_id}",
        )

    def credit_tournament_level(self, user_id: int, amount: int, room_id: str) -> Wallet:
        return self._apply(
            user_id,
            amount,
            "tournament_level_reward",
            room_id,
            f"Tournament level complete bonus for room {room_id}",
            idempotency_key=f"tournament_level:{room_id}:{user_id}",
        )

    def credit_level_complete(
        self,
        user_id: int,
        amount: int,
        level_number: int,
        *,
        completion_id: str,
    ) -> Wallet:
        # transaction_id column is CHAR(36) — use the UUID only, not a composite key.
        return self._apply(
            user_id,
            amount,
            "level_complete_reward",
            str(level_number),
            f"Level {level_number} completion reward",
            idempotency_key=completion_id,
        )
