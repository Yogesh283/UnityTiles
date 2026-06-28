from sqlalchemy.orm import Session

from database.models import IapPurchase
from payments.catalog import get_product
from payments.google_play import GooglePlayVerifier
from wallet.service import WalletService


class PaymentService:
    def __init__(self, db: Session):
        self.db = db
        self._verifier = GooglePlayVerifier()

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

        if verified.consumption_state == 0:
            try:
                self._verifier.consume_product(product_id, purchase_token)
            except ValueError:
                pass

        return {
            "already_processed": False,
            "order_id": verified.order_id,
            "product_id": product.product_id,
            "coins_added": product.coins,
            "balance": wallet.balance,
        }
