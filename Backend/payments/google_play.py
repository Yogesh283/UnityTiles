from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError

from config import get_settings

ANDROID_PUBLISHER_SCOPE = "https://www.googleapis.com/auth/androidpublisher"


@dataclass
class VerifiedPurchase:
    order_id: str
    product_id: str
    purchase_token: str
    purchase_state: int
    consumption_state: int


class GooglePlayVerifier:
    def __init__(self) -> None:
        settings = get_settings()
        self._package_name = settings.google_play_package_name
        self._service = None

        if settings.google_play_service_account_json:
            creds_path = Path(settings.google_play_service_account_json)
            if creds_path.is_file():
                credentials = service_account.Credentials.from_service_account_file(
                    str(creds_path),
                    scopes=[ANDROID_PUBLISHER_SCOPE],
                )
                self._service = build("androidpublisher", "v3", credentials=credentials, cache_discovery=False)

    @property
    def is_configured(self) -> bool:
        return self._service is not None

    def verify_product(self, product_id: str, purchase_token: str) -> VerifiedPurchase:
        if not self._service:
            raise RuntimeError("Google Play billing is not configured on the server")

        try:
            result = (
                self._service.purchases()
                .products()
                .get(
                    packageName=self._package_name,
                    productId=product_id,
                    token=purchase_token,
                )
                .execute()
            )
        except HttpError as exc:
            detail = _extract_http_error(exc)
            raise ValueError(f"Google Play verification failed: {detail}") from exc

        purchase_state = int(result.get("purchaseState", -1))
        if purchase_state != 0:
            raise ValueError("Purchase is not completed")

        order_id = result.get("orderId")
        if not order_id:
            raise ValueError("Missing order ID from Google Play")

        return VerifiedPurchase(
            order_id=order_id,
            product_id=product_id,
            purchase_token=purchase_token,
            purchase_state=purchase_state,
            consumption_state=int(result.get("consumptionState", 0)),
        )

    def consume_product(self, product_id: str, purchase_token: str) -> None:
        if not self._service:
            raise RuntimeError("Google Play billing is not configured on the server")

        try:
            self._service.purchases().products().consume(
                packageName=self._package_name,
                productId=product_id,
                token=purchase_token,
            ).execute()
        except HttpError as exc:
            detail = _extract_http_error(exc)
            raise ValueError(f"Google Play consume failed: {detail}") from exc


def _extract_http_error(exc: HttpError) -> str:
    try:
        payload = json.loads(exc.content.decode("utf-8"))
        return payload.get("error", {}).get("message", str(exc))
    except Exception:
        return str(exc)
