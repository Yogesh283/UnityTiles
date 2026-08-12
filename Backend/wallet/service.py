import hashlib
import re
import uuid
from decimal import Decimal, ROUND_DOWN

from sqlalchemy import func
from sqlalchemy.orm import Session

from core.audit import write_audit_log
from core.identifiers import new_transaction_id
from database.models import User, Wallet, WalletTransaction, WithdrawalRequest


class WalletService:
    USDT_COINS_PER_UNIT = 100
    MIN_WITHDRAW_COINS = 100
    SIGNUP_BONUS_COINS = 100
    BONUS_ENTRY_PERCENT = 20
    BEP20_RE = re.compile(r"^0x[a-fA-F0-9]{40}$")

    def __init__(self, db: Session):
        self.db = db

    def get_balance(self, user_id: int) -> int:
        wallet = self.db.query(Wallet).filter(Wallet.user_id == user_id).first()
        return wallet.balance if wallet else 0

    def get_bonus_balance(self, user_id: int) -> int:
        wallet = self.db.query(Wallet).filter(Wallet.user_id == user_id).first()
        return int(getattr(wallet, "bonus_balance", 0) or 0) if wallet else 0

    def snapshot(self, user_id: int) -> dict:
        wallet = self.ensure_wallet(user_id)
        return {
            "balance": int(wallet.balance or 0),
            "bonus_balance": int(getattr(wallet, "bonus_balance", 0) or 0),
        }

    def ensure_wallet(self, user_id: int) -> Wallet:
        wallet = self.db.query(Wallet).filter(Wallet.user_id == user_id).first()
        if wallet:
            if getattr(wallet, "bonus_balance", None) is None:
                wallet.bonus_balance = 0
            return wallet
        wallet = Wallet(user_id=user_id, balance=0, bonus_balance=0)
        self.db.add(wallet)
        self.db.flush()
        return wallet

    @classmethod
    def split_tournament_entry(cls, amount: int, bonus_balance: int) -> tuple[int, int]:
        """While bonus > 0: up to 20% bonus + rest main. Bonus empty → 100% main."""
        amount = max(0, int(amount))
        bonus_balance = max(0, int(bonus_balance))
        if amount <= 0:
            return 0, 0
        if bonus_balance <= 0:
            return amount, 0
        bonus_use = min(bonus_balance, (amount * cls.BONUS_ENTRY_PERCENT) // 100)
        return amount - bonus_use, bonus_use

    def can_afford_tournament(self, user_id: int, amount: int) -> bool:
        wallet = self.ensure_wallet(user_id)
        main_use, _bonus_use = self.split_tournament_entry(
            amount, int(getattr(wallet, "bonus_balance", 0) or 0)
        )
        return int(wallet.balance or 0) >= main_use

    def _apply(
        self,
        user_id: int,
        amount: int,
        tx_type: str,
        reference_id: str | None,
        reason: str,
        *,
        idempotency_key: str | None = None,
        commit: bool = True,
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
        if commit:
            self.db.commit()
            self.db.refresh(wallet)
        return wallet

    def credit_signup_bonus(self, user_id: int, amount: int | None = None) -> Wallet:
        """New-register bonus → bonus wallet only. Not withdrawable."""
        amount = self.SIGNUP_BONUS_COINS if amount is None else max(0, int(amount))
        wallet = self.ensure_wallet(user_id)
        if amount <= 0:
            return wallet
        key = hashlib.sha256(f"signup_bonus:{user_id}".encode()).hexdigest()[:36]
        existing = (
            self.db.query(WalletTransaction)
            .filter(WalletTransaction.transaction_id == key)
            .first()
        )
        if existing:
            return wallet

        wallet = (
            self.db.query(Wallet).filter(Wallet.user_id == user_id).with_for_update().first()
        )
        if not wallet:
            raise ValueError("Wallet not found")
        before = int(getattr(wallet, "bonus_balance", 0) or 0)
        wallet.bonus_balance = before + amount
        self.db.add(
            WalletTransaction(
                transaction_id=key,
                user_id=user_id,
                amount=amount,
                balance_before=wallet.balance,
                balance_after=wallet.balance,
                type="signup_bonus",
                reference_id=str(user_id),
                note=f"Signup bonus wallet {before}→{wallet.bonus_balance}",
                reason="New user signup bonus (tournament only, no withdraw)",
            )
        )
        self.db.commit()
        self.db.refresh(wallet)
        return wallet

    def deduct_entry_fee(self, user_id: int, amount: int, room_id: str) -> Wallet:
        amount = int(amount)
        wallet = self.ensure_wallet(user_id)
        if amount <= 0:
            return wallet

        main_key = f"entry:{room_id}:{user_id}"
        existing = (
            self.db.query(WalletTransaction)
            .filter(WalletTransaction.transaction_id == main_key)
            .first()
        )
        if existing:
            return wallet

        wallet = (
            self.db.query(Wallet).filter(Wallet.user_id == user_id).with_for_update().first()
        )
        if not wallet:
            raise ValueError("Wallet not found")

        bonus_before = int(getattr(wallet, "bonus_balance", 0) or 0)
        main_use, bonus_use = self.split_tournament_entry(amount, bonus_before)
        if int(wallet.balance or 0) < main_use:
            raise ValueError("Insufficient balance")

        main_before = int(wallet.balance or 0)
        wallet.balance = main_before - main_use
        wallet.bonus_balance = bonus_before - bonus_use

        self.db.add(
            WalletTransaction(
                transaction_id=main_key,
                user_id=user_id,
                amount=-main_use,
                balance_before=main_before,
                balance_after=wallet.balance,
                type="tournament_entry",
                reference_id=room_id,
                note=f"Tournament entry {main_use} main + {bonus_use} bonus",
                reason=f"Tournament entry fee for room {room_id}",
            )
        )
        if bonus_use > 0:
            bonus_key = hashlib.sha256(f"entry_bonus:{room_id}:{user_id}".encode()).hexdigest()[:36]
            self.db.add(
                WalletTransaction(
                    transaction_id=bonus_key,
                    user_id=user_id,
                    amount=-bonus_use,
                    balance_before=wallet.balance,
                    balance_after=wallet.balance,
                    type="tournament_entry_bonus",
                    reference_id=room_id,
                    note=f"Bonus wallet {bonus_before}→{wallet.bonus_balance}",
                    reason=f"Tournament bonus share for room {room_id}",
                )
            )
        write_audit_log(
            self.db,
            action="wallet_change",
            message=f"tournament_entry: -{amount} (main {main_use}, bonus {bonus_use})",
            actor_type="user",
            actor_id=str(user_id),
            target_type="wallet",
            target_id=str(user_id),
            context={
                "amount": -amount,
                "main_use": main_use,
                "bonus_use": bonus_use,
                "balance_after": wallet.balance,
                "bonus_after": wallet.bonus_balance,
                "reference_id": room_id,
            },
        )
        self.db.commit()
        self.db.refresh(wallet)
        return wallet

    def refund_entry_fee(self, user_id: int, amount: int, reference_id: str) -> Wallet:
        wallet = self.ensure_wallet(user_id)
        already = (
            self.db.query(WalletTransaction)
            .filter(
                WalletTransaction.user_id == user_id,
                WalletTransaction.reference_id == reference_id,
                WalletTransaction.type == "tournament_entry_refund",
            )
            .first()
        )
        if already:
            return wallet

        main_tx = (
            self.db.query(WalletTransaction)
            .filter(
                WalletTransaction.user_id == user_id,
                WalletTransaction.reference_id == reference_id,
                WalletTransaction.type == "tournament_entry",
            )
            .first()
        )
        bonus_tx = (
            self.db.query(WalletTransaction)
            .filter(
                WalletTransaction.user_id == user_id,
                WalletTransaction.reference_id == reference_id,
                WalletTransaction.type == "tournament_entry_bonus",
            )
            .first()
        )
        if main_tx:
            main_refund = abs(int(main_tx.amount))
            bonus_refund = abs(int(bonus_tx.amount)) if bonus_tx else 0
        elif bonus_tx:
            main_refund = 0
            bonus_refund = abs(int(bonus_tx.amount))
        else:
            main_refund = max(0, int(amount))
            bonus_refund = 0

        wallet = (
            self.db.query(Wallet).filter(Wallet.user_id == user_id).with_for_update().first()
        )
        if not wallet:
            raise ValueError("Wallet not found")

        main_before = int(wallet.balance or 0)
        bonus_before = int(getattr(wallet, "bonus_balance", 0) or 0)
        wallet.balance = main_before + main_refund
        wallet.bonus_balance = bonus_before + bonus_refund

        refund_key = f"refund:{reference_id}:{user_id}"
        if len(refund_key) > 36:
            refund_key = hashlib.sha256(refund_key.encode()).hexdigest()[:36]
        self.db.add(
            WalletTransaction(
                transaction_id=refund_key,
                user_id=user_id,
                amount=main_refund,
                balance_before=main_before,
                balance_after=wallet.balance,
                type="tournament_entry_refund",
                reference_id=reference_id,
                note=f"Refund {main_refund} main + {bonus_refund} bonus",
                reason=f"Refund tournament entry ({reference_id})",
            )
        )
        if bonus_refund > 0:
            bonus_key = hashlib.sha256(f"refund_bonus:{reference_id}:{user_id}".encode()).hexdigest()[:36]
            self.db.add(
                WalletTransaction(
                    transaction_id=bonus_key,
                    user_id=user_id,
                    amount=bonus_refund,
                    balance_before=wallet.balance,
                    balance_after=wallet.balance,
                    type="tournament_entry_bonus_refund",
                    reference_id=reference_id,
                    note=f"Bonus wallet {bonus_before}→{wallet.bonus_balance}",
                    reason=f"Refund tournament bonus ({reference_id})",
                )
            )
        self.db.commit()
        self.db.refresh(wallet)
        return wallet

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

    def credit_referral(
        self,
        user_id: int,
        amount: int,
        *,
        room_id: str,
        level: int,
        from_user_id: int,
    ) -> Wallet:
        # transaction_id column is CHAR(36) — hash the composite key to fit.
        raw = f"referral:{room_id}:{from_user_id}:{user_id}:{level}"
        idempotency_key = hashlib.sha256(raw.encode()).hexdigest()[:36]
        return self._apply(
            user_id,
            amount,
            f"referral_l{level}",
            room_id,
            f"WXO Points level {level} from user {from_user_id}",
            idempotency_key=idempotency_key,
        )

    def deduct_pool_entry(self, user_id: int, amount: int, pool_id: int) -> Wallet:
        return self._apply(
            user_id,
            -amount,
            "pool_entry",
            str(pool_id),
            f"Pool entry fee for pool {pool_id}",
            idempotency_key=f"pool_entry:{pool_id}:{user_id}",
        )

    def credit_pool_prize(self, user_id: int, amount: int, pool_id: int, rank: int) -> Wallet:
        idempotency_key = hashlib.sha256(f"pool_prize:{pool_id}:{user_id}".encode()).hexdigest()[:36]
        return self._apply(
            user_id,
            amount,
            "pool_prize",
            str(pool_id),
            f"Pool prize rank {rank} in pool {pool_id}",
            idempotency_key=idempotency_key,
        )

    def refund_pool_entry(self, user_id: int, amount: int, pool_id: int) -> Wallet:
        return self._apply(
            user_id,
            amount,
            "pool_entry_refund",
            str(pool_id),
            f"Refund pool entry for pool {pool_id}",
            idempotency_key=f"pool_refund:{pool_id}:{user_id}",
        )

    def admin_adjust(self, user_id: int, amount: int, reason: str) -> Wallet:
        return self._apply(user_id, amount, "admin_adjust", None, reason)

    def withdraw_methods(self) -> dict:
        return {
            "usdt_bep20": {
                "label": "USDT BEP20",
                "network": "BEP20",
                "currency": "USDT",
                "rate": f"{self.USDT_COINS_PER_UNIT} WXO = 1 USDT",
                "coins_per_usdt": self.USDT_COINS_PER_UNIT,
                "min_coins": self.MIN_WITHDRAW_COINS,
                "approval": "admin",
            }
        }

    def request_usdt_withdraw(self, user: User, coins: int, bep20_address: str) -> dict:
        coins = int(coins)
        if coins < self.MIN_WITHDRAW_COINS:
            raise ValueError(f"Minimum withdrawal is {self.MIN_WITHDRAW_COINS} WXO (1 USDT)")

        addr = (bep20_address or "").strip()
        if not self.BEP20_RE.fullmatch(addr):
            raise ValueError("Enter a valid USDT BEP20 address (0x + 40 hex)")

        pending = (
            self.db.query(WithdrawalRequest)
            .filter(
                WithdrawalRequest.user_id == user.id,
                WithdrawalRequest.status == "pending",
            )
            .first()
        )
        if pending:
            raise ValueError("You already have a pending withdrawal. Wait for admin approval.")

        self.ensure_wallet(user.id)
        usdt = (Decimal(coins) / Decimal(self.USDT_COINS_PER_UNIT)).quantize(
            Decimal("0.00000001"), rounding=ROUND_DOWN
        )
        code = "WD" + uuid.uuid4().hex[:16].upper()
        row = WithdrawalRequest(
            request_code=code,
            user_id=user.id,
            coins=coins,
            usdt_amount=usdt,
            bep20_address=addr,
            status="pending",
        )
        self.db.add(row)
        self.db.flush()
        try:
            wallet = self._apply(
                user.id,
                -coins,
                "withdraw_usdt",
                code,
                f"USDT BEP20 withdraw {code}",
                idempotency_key=hashlib.sha256(f"wd:{code}".encode()).hexdigest()[:36],
            )
        except ValueError:
            self.db.rollback()
            raise

        self.db.refresh(row)
        return {
            "request_code": code,
            "coins": coins,
            "usdt_amount": float(usdt),
            "bep20_address": addr,
            "status": "pending",
            "network": "BEP20",
            "currency": "USDT",
            "balance": wallet.balance,
            "bonus_balance": int(getattr(wallet, "bonus_balance", 0) or 0),
            "created_at": row.created_at,
        }
        # refresh after commit so created_at is populated
        self.db.refresh(row)
        out["created_at"] = row.created_at
        return out

    def list_withdrawals(self, user_id: int, limit: int = 30) -> list[dict]:
        rows = (
            self.db.query(WithdrawalRequest)
            .filter(WithdrawalRequest.user_id == user_id)
            .order_by(WithdrawalRequest.created_at.desc())
            .limit(limit)
            .all()
        )
        return [self._public_withdrawal(row) for row in rows]

    @staticmethod
    def _public_withdrawal(row: WithdrawalRequest) -> dict:
        return {
            "request_code": row.request_code,
            "coins": int(row.coins),
            "usdt_amount": float(row.usdt_amount or 0),
            "bep20_address": row.bep20_address,
            "status": row.status,
            "admin_note": row.admin_note,
            "payout_tx": row.payout_tx,
            "network": "BEP20",
            "currency": "USDT",
            "created_at": row.created_at,
            "reviewed_at": row.reviewed_at,
        }

    def credit_deposit(self, user_id: int, amount: int, *, method: str, order_code: str) -> Wallet:
        tx_type = "deposit_usdt" if method == "usdt_bep20" else "deposit_upi"
        idempotency_key = hashlib.sha256(f"dep:{order_code}".encode()).hexdigest()[:36]
        return self._apply(
            user_id,
            amount,
            tx_type,
            order_code,
            f"Deposit {method} {order_code}",
            idempotency_key=idempotency_key,
        )

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

    def find_transfer_user(self, query: str) -> User | None:
        """Resolve a P2P recipient by numeric id, UUID, username, or referral code."""
        q = (query or "").strip()
        if not q:
            return None
        if q.isdigit():
            user = self.db.query(User).filter(User.id == int(q)).first()
            if user:
                return user
        user = self.db.query(User).filter(User.user_uuid == q).first()
        if user:
            return user
        user = (
            self.db.query(User)
            .filter(func.lower(User.username) == q.lower())
            .first()
        )
        if user:
            return user
        return (
            self.db.query(User)
            .filter(func.upper(User.referral_code) == q.upper())
            .first()
        )

    def p2p_transfer(self, sender: User, recipient: User, amount: int, note: str | None = None) -> dict:
        """Move WXO coins from sender to recipient in one atomic wallet transaction."""
        amount = int(amount)
        if amount < 1:
            raise ValueError("Minimum transfer is 1 WXO")
        if sender.id == recipient.id:
            raise ValueError("You cannot transfer to yourself")
        if not recipient.is_active or recipient.is_banned:
            raise ValueError("This user cannot receive transfers")

        self.ensure_wallet(sender.id)
        self.ensure_wallet(recipient.id)

        first_id, second_id = sorted((sender.id, recipient.id))
        wallets = (
            self.db.query(Wallet)
            .filter(Wallet.user_id.in_((first_id, second_id)))
            .order_by(Wallet.user_id.asc())
            .with_for_update()
            .all()
        )
        by_id = {w.user_id: w for w in wallets}
        from_w = by_id.get(sender.id)
        to_w = by_id.get(recipient.id)
        if not from_w or not to_w:
            raise ValueError("Wallet not found")
        if from_w.balance < amount:
            raise ValueError("Insufficient balance")

        send_before = from_w.balance
        recv_before = to_w.balance
        from_w.balance -= amount
        to_w.balance += amount

        pair_id = new_transaction_id()
        clean_note = (note or "").strip()[:200]
        send_reason = clean_note or f"P2P to {recipient.display_name or recipient.id}"
        recv_reason = clean_note or f"P2P from {sender.display_name or sender.id}"

        self.db.add(
            WalletTransaction(
                transaction_id=new_transaction_id(),
                user_id=sender.id,
                amount=-amount,
                balance_before=send_before,
                balance_after=from_w.balance,
                type="p2p_send",
                reference_id=str(recipient.id),
                note=send_reason,
                reason=f"p2p:{pair_id}",
            )
        )
        self.db.add(
            WalletTransaction(
                transaction_id=new_transaction_id(),
                user_id=recipient.id,
                amount=amount,
                balance_before=recv_before,
                balance_after=to_w.balance,
                type="p2p_receive",
                reference_id=str(sender.id),
                note=recv_reason,
                reason=f"p2p:{pair_id}",
            )
        )
        write_audit_log(
            self.db,
            action="p2p_transfer",
            message=f"P2P {amount} WXO {sender.id} → {recipient.id}",
            actor_type="user",
            actor_id=str(sender.id),
            target_type="user",
            target_id=str(recipient.id),
            context={"amount": amount, "pair_id": pair_id, "note": clean_note or None},
        )
        self.db.commit()

        return {
            "ok": True,
            "amount": amount,
            "balance": from_w.balance,
            "bonus_balance": int(getattr(from_w, "bonus_balance", 0) or 0),
            "transaction_id": pair_id,
            "to": {
                "user_id": recipient.id,
                "display_name": recipient.display_name or "Player",
            },
        }
