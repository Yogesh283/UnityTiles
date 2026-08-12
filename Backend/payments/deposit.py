"""WXO deposits: UPI via Razorpay, USDT BEP20 via NowPayments."""

from __future__ import annotations

import hashlib
import hmac
import json
import logging
import uuid
from datetime import datetime
from decimal import Decimal, ROUND_DOWN

import httpx
from sqlalchemy.orm import Session

from config import get_settings
from database.models import DepositOrder, User
from wallet.service import WalletService

logger = logging.getLogger(__name__)

UPI_COINS_PER_INR = 1
USDT_COINS_PER_UNIT = 100
MIN_INR = 10
MAX_INR = 100_000
MIN_USDT = Decimal("1")
MAX_USDT = Decimal("10000")

NOWPAYMENTS_API = "https://api.nowpayments.io/v1"
RAZORPAY_ORDERS = "https://api.razorpay.com/v1/orders"


def _new_order_code() -> str:
    return "WXO" + uuid.uuid4().hex[:16].upper()


def _public_site() -> str:
    return get_settings().public_site_url.rstrip("/")


def _api_v1_base() -> str:
    base = get_settings().api_base_url.rstrip("/")
    if base.endswith("/api/v1"):
        return base
    return base + "/api/v1"


class DepositService:
    def __init__(self, db: Session):
        self.db = db
        self.settings = get_settings()

    def methods(self) -> dict:
        return {
            "upi": {
                "label": "UPI",
                "gateway": "razorpay",
                "currency": "INR",
                "rate": f"₹{UPI_COINS_PER_INR} = {UPI_COINS_PER_INR} WXO",
                "coins_per_unit": UPI_COINS_PER_INR,
                "min": MIN_INR,
                "max": MAX_INR,
                "configured": bool(self.settings.razorpay_key_id and self.settings.razorpay_key_secret),
            },
            "usdt_bep20": {
                "label": "USDT BEP20",
                "gateway": "nowpayments",
                "currency": "USDT",
                "network": "BEP20",
                "rate": f"1 USDT = {USDT_COINS_PER_UNIT} WXO",
                "coins_per_unit": USDT_COINS_PER_UNIT,
                "min": float(MIN_USDT),
                "max": float(MAX_USDT),
                "configured": bool(self.settings.nowpayments_api_key),
            },
        }

    def create(self, user: User, method: str, amount: float) -> dict:
        method = (method or "").strip().lower()
        if method in ("usdt", "usdtbsc", "bep20"):
            method = "usdt_bep20"
        if method == "upi":
            return self._create_upi(user, amount)
        if method == "usdt_bep20":
            return self._create_usdt(user, amount)
        raise ValueError("Choose UPI or USDT BEP20")

    def _create_upi(self, user: User, amount: float) -> dict:
        if not (self.settings.razorpay_key_id and self.settings.razorpay_key_secret):
            raise ValueError("UPI gateway is not configured yet")
        inr = int(Decimal(str(amount)).quantize(Decimal("1"), rounding=ROUND_DOWN))
        if inr < MIN_INR:
            raise ValueError(f"Minimum UPI deposit is ₹{MIN_INR}")
        if inr > MAX_INR:
            raise ValueError(f"Maximum UPI deposit is ₹{MAX_INR:,}")
        coins = inr * UPI_COINS_PER_INR
        code = _new_order_code()
        paise = inr * 100

        with httpx.Client(timeout=20.0) as client:
            res = client.post(
                RAZORPAY_ORDERS,
                auth=(self.settings.razorpay_key_id, self.settings.razorpay_key_secret),
                json={
                    "amount": paise,
                    "currency": "INR",
                    "receipt": code[:40],
                    "payment_capture": 1,
                    "notes": {"user_id": str(user.id), "order_code": code, "coins": str(coins)},
                },
            )
        if res.status_code >= 400:
            logger.warning("Razorpay order failed: %s", res.text)
            raise ValueError("Could not start UPI payment. Try again.")
        data = res.json()
        rzp_order = data.get("id")
        if not rzp_order:
            raise ValueError("UPI gateway did not return an order")

        order = DepositOrder(
            order_code=code,
            user_id=user.id,
            method="upi",
            provider="razorpay",
            pay_amount=inr,
            pay_currency="INR",
            coins=coins,
            status="pending",
            provider_order_id=rzp_order,
        )
        self.db.add(order)
        self.db.commit()
        self.db.refresh(order)

        return {
            "checkout": "razorpay",
            "order_code": code,
            "coins": coins,
            "pay_amount": inr,
            "pay_currency": "INR",
            "razorpay_key": self.settings.razorpay_key_id,
            "razorpay_order_id": rzp_order,
            "amount_paise": paise,
            "prefill_name": user.display_name or "Player",
            "prefill_email": user.email or "",
        }

    def _create_usdt(self, user: User, amount: float) -> dict:
        if not self.settings.nowpayments_api_key:
            raise ValueError("USDT gateway is not configured yet")
        usdt = Decimal(str(amount)).quantize(Decimal("0.01"), rounding=ROUND_DOWN)
        if usdt < MIN_USDT:
            raise ValueError(f"Minimum USDT deposit is {MIN_USDT} USDT")
        if usdt > MAX_USDT:
            raise ValueError(f"Maximum USDT deposit is {MAX_USDT} USDT")
        coins = int(usdt * USDT_COINS_PER_UNIT)
        code = _new_order_code()
        site = _public_site()
        api = _api_v1_base()

        payload = {
            "price_amount": float(usdt),
            "price_currency": "usd",
            "pay_currency": "usdtbsc",
            "order_id": code,
            "order_description": f"WXO deposit {coins} coins",
            "ipn_callback_url": f"{api}/payments/webhooks/nowpayments",
            "success_url": f"{site}/wallet.html?deposit=ok&order={code}",
            "cancel_url": f"{site}/wallet.html?deposit=cancel&order={code}",
            "is_fixed_rate": True,
        }
        with httpx.Client(timeout=25.0) as client:
            res = client.post(
                f"{NOWPAYMENTS_API}/invoice",
                headers={
                    "x-api-key": self.settings.nowpayments_api_key,
                    "Content-Type": "application/json",
                },
                json=payload,
            )
        if res.status_code >= 400:
            logger.warning("NowPayments invoice failed: %s", res.text)
            raise ValueError("Could not start USDT payment. Try again.")
        data = res.json()
        invoice_url = data.get("invoice_url")
        invoice_id = str(data.get("id") or data.get("invoice_id") or "")
        if not invoice_url:
            raise ValueError("USDT gateway did not return a checkout URL")

        order = DepositOrder(
            order_code=code,
            user_id=user.id,
            method="usdt_bep20",
            provider="nowpayments",
            pay_amount=float(usdt),
            pay_currency="USDT",
            coins=coins,
            status="pending",
            provider_order_id=invoice_id or code,
            checkout_url=invoice_url,
            extra=json.dumps({"invoice": data}, default=str)[:4000],
        )
        self.db.add(order)
        self.db.commit()
        self.db.refresh(order)

        return {
            "checkout": "nowpayments",
            "order_code": code,
            "coins": coins,
            "pay_amount": float(usdt),
            "pay_currency": "USDT",
            "invoice_url": invoice_url,
        }

    def verify_razorpay(self, user: User, razorpay_order_id: str, razorpay_payment_id: str, signature: str) -> dict:
        if not self.settings.razorpay_key_secret:
            raise ValueError("UPI gateway is not configured yet")
        msg = f"{razorpay_order_id}|{razorpay_payment_id}"
        expected = hmac.new(
            self.settings.razorpay_key_secret.encode(),
            msg.encode(),
            hashlib.sha256,
        ).hexdigest()
        if not hmac.compare_digest(expected, signature or ""):
            raise ValueError("Invalid UPI payment signature")

        order = (
            self.db.query(DepositOrder)
            .filter(
                DepositOrder.provider_order_id == razorpay_order_id,
                DepositOrder.user_id == user.id,
                DepositOrder.method == "upi",
            )
            .first()
        )
        if not order:
            raise ValueError("Deposit order not found")
        return self._mark_paid(order, razorpay_payment_id)

    def handle_razorpay_webhook(self, body: bytes, signature: str) -> dict:
        secret = self.settings.razorpay_webhook_secret or self.settings.razorpay_key_secret
        if not secret:
            raise ValueError("Razorpay webhook secret missing")
        expected = hmac.new(secret.encode(), body, hashlib.sha256).hexdigest()
        if not hmac.compare_digest(expected, signature or ""):
            raise ValueError("Invalid Razorpay webhook signature")
        payload = json.loads(body.decode("utf-8") or "{}")
        event = payload.get("event")
        payment = ((payload.get("payload") or {}).get("payment") or {}).get("entity") or {}
        if event not in ("payment.captured", "order.paid"):
            return {"ok": True, "ignored": event}
        rzp_order = payment.get("order_id") or ((payload.get("payload") or {}).get("order") or {}).get("entity", {}).get("id")
        rzp_payment = payment.get("id")
        if not rzp_order:
            return {"ok": True, "ignored": "no_order"}
        order = self.db.query(DepositOrder).filter(DepositOrder.provider_order_id == rzp_order).first()
        if not order:
            return {"ok": True, "ignored": "unknown_order"}
        return self._mark_paid(order, rzp_payment or rzp_order)

    def handle_nowpayments_ipn(self, body: dict, signature: str) -> dict:
        secret = self.settings.nowpayments_ipn_secret
        if secret:
            if not _verify_nowpayments_sig(body, signature, secret):
                raise ValueError("Invalid NowPayments signature")
        status = str(body.get("payment_status") or "").lower()
        order_code = str(body.get("order_id") or "")
        payment_id = str(body.get("payment_id") or body.get("invoice_id") or "")
        if not order_code:
            return {"ok": True, "ignored": "no_order"}
        order = self.db.query(DepositOrder).filter(DepositOrder.order_code == order_code).first()
        if not order:
            return {"ok": True, "ignored": "unknown_order"}
        if status in ("failed", "expired", "refunded"):
            if order.status == "pending":
                order.status = status
                self.db.commit()
            return {"ok": True, "status": order.status}
        if status not in ("finished", "confirmed"):
            return {"ok": True, "status": status}
        return self._mark_paid(order, payment_id or order.provider_order_id)

    def get_order(self, user_id: int, order_code: str) -> dict:
        order = (
            self.db.query(DepositOrder)
            .filter(DepositOrder.order_code == order_code, DepositOrder.user_id == user_id)
            .first()
        )
        if not order:
            raise ValueError("Deposit order not found")
        return {
            "order_code": order.order_code,
            "method": order.method,
            "status": order.status,
            "coins": order.coins,
            "pay_amount": float(order.pay_amount),
            "pay_currency": order.pay_currency,
            "checkout_url": order.checkout_url,
        }

    def _mark_paid(self, order: DepositOrder, provider_payment_id: str | None) -> dict:
        if order.status == "paid":
            balance = WalletService(self.db).get_balance(order.user_id)
            return {
                "already_processed": True,
                "order_code": order.order_code,
                "coins": order.coins,
                "balance": balance,
                "status": "paid",
            }
        wallet = WalletService(self.db).credit_deposit(
            order.user_id,
            order.coins,
            method=order.method,
            order_code=order.order_code,
        )
        order.status = "paid"
        order.provider_payment_id = provider_payment_id
        order.paid_at = datetime.utcnow()
        self.db.commit()
        return {
            "already_processed": False,
            "order_code": order.order_code,
            "coins": order.coins,
            "balance": wallet.balance,
            "status": "paid",
        }


def _sort_keys(value):
    if isinstance(value, dict):
        return {k: _sort_keys(v) for k, v in sorted(value.items())}
    if isinstance(value, list):
        return [_sort_keys(v) for v in value]
    return value


def _verify_nowpayments_sig(body: dict, signature: str, secret: str) -> bool:
    if not signature:
        return False
    message = json.dumps(_sort_keys(body), separators=(",", ":"), ensure_ascii=False)
    digest = hmac.new(secret.encode(), message.encode(), hashlib.sha512).hexdigest()
    return hmac.compare_digest(digest, signature)
