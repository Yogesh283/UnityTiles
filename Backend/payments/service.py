from __future__ import annotations

import logging

from sqlalchemy.orm import Session

from core.audit import write_audit_log
from database.models import IapPurchase
from payments.catalog import get_product
from payments.google_play import get_google_play_verifier
from wallet.service import WalletService

logger = logging.getLogger(__name__)


class PaymentService:
    def __init__(self, db: Session):
        self.db = db
        self._verifier = get_google_play_verifier()

    def verify_google_play_purchase(self, user_id: int, product_id: str, purchase_token: str) -> dict:
        product = get_product(product_id)
        if not product:
            raise ValueError("Unknown product")

        existing = (
            self.db.query(IapPurchase)
            .filter(IapPurchase.purchase_token == purchase_token)
            .first()
        )
        if existing:
            balance = WalletService(self.db).get_balance(user_id)
            return {
                "already_processed": True,
                "order_id": existing.order_id,
                "product_id": existing.product_id,
                "coins_added": existing.coins_added,
                "balance": balance,
            }

        verified = self._verifier.verify_product(product_id, purchase_token)

        duplicate_order = (
            self.db.query(IapPurchase)
            .filter(IapPurchase.order_id == verified.order_id)
            .first()
        )
        if duplicate_order:
            balance = WalletService(self.db).get_balance(user_id)
            return {
                "already_processed": True,
                "order_id": duplicate_order.order_id,
                "product_id": duplicate_order.product_id,
                "coins_added": duplicate_order.coins_added,
                "balance": balance,
            }

        wallet = WalletService(self.db).credit_iap(
            user_id=user_id,
            amount=product.coins,
            order_id=verified.order_id,
            product_id=product.product_id,
        )

        self.db.add(
            IapPurchase(
                user_id=user_id,
                platform="google_play",
                product_id=product.product_id,
                order_id=verified.order_id,
                purchase_token=purchase_token,
                coins_added=product.coins,
            )
        )
        self.db.commit()

        write_audit_log(
            self.db,
            action="iap_purchase",
            message=f"Google Play purchase: {product.product_id} (+{product.coins} coins)",
            actor_type="user",
            actor_id=str(user_id),
            target_type="iap_order",
            target_id=verified.order_id,
            context={
                "product_id": product.product_id,
                "coins_added": product.coins,
                "platform": "google_play",
            },
        )
        self.db.commit()

        if verified.consumption_state == 0:
            try:
                self._verifier.consume_product(product_id, purchase_token)
            except ValueError as exc:
                logger.warning(
                    "Google Play consume failed for order %s: %s",
                    verified.order_id,
                    exc,
                )

        return {
            "already_processed": False,
            "order_id": verified.order_id,
            "product_id": product.product_id,
            "coins_added": product.coins,
            "balance": wallet.balance,
        }
