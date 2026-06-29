from __future__ import annotations

import json
import logging
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path

from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError

from config import get_settings

logger = logging.getLogger(__name__)

ANDROID_PUBLISHER_SCOPE = "https://www.googleapis.com/auth/androidpublisher"
BACKEND_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_CREDENTIALS_PATH = BACKEND_ROOT / "secrets" / "google-play-service-account.json"


@dataclass
class VerifiedPurchase:
    order_id: str
    product_id: str
    purchase_token: str
    purchase_state: int
    consumption_state: int


def _resolve_credentials_path(raw: str) -> Path | None:
    candidate = Path(raw)
    if candidate.is_file():
        return candidate

    relative = BACKEND_ROOT / raw
    if relative.is_file():
        return relative

    return None


def _load_credentials():
    settings = get_settings()
    raw = settings.google_play_service_account_json.strip()

    if raw.startswith("{"):
        try:
            info = json.loads(raw)
            return service_account.Credentials.from_service_account_info(
                info,
                scopes=[ANDROID_PUBLISHER_SCOPE],
            )
        except json.JSONDecodeError as exc:
            logger.error("Invalid inline Google Play JSON: %s", exc)
            return _load_default_credentials()

    if raw:
        path = _resolve_credentials_path(raw)
        if path:
            return service_account.Credentials.from_service_account_file(
                str(path),
                scopes=[ANDROID_PUBLISHER_SCOPE],
            )
        logger.warning(
            "Google Play credentials path not found: %s — trying default file",
            raw,
        )

    return _load_default_credentials()


def _load_default_credentials():
    if DEFAULT_CREDENTIALS_PATH.is_file():
        return service_account.Credentials.from_service_account_file(
            str(DEFAULT_CREDENTIALS_PATH),
            scopes=[ANDROID_PUBLISHER_SCOPE],
        )
    return None


class GooglePlayVerifier:
    def __init__(self) -> None:
        settings = get_settings()
        self._package_name = settings.google_play_package_name
        self._service = None
        self._config_error: str | None = None

        try:
            credentials = _load_credentials()
            if credentials:
                self._service = build(
                    "androidpublisher",
                    "v3",
                    credentials=credentials,
                    cache_discovery=False,
                )
            else:
                self._config_error = (
                    "Set GOOGLE_PLAY_SERVICE_ACCOUNT_JSON to a JSON file path, "
                    f"or place credentials at {DEFAULT_CREDENTIALS_PATH}"
                )
        except Exception as exc:
            self._config_error = f"Invalid Google Play credentials: {exc}"
            logger.exception("Failed to initialize Google Play verifier")

    @property
    def is_configured(self) -> bool:
        return self._service is not None

    @property
    def config_error(self) -> str | None:
        return self._config_error

    def verify_product(self, product_id: str, purchase_token: str) -> VerifiedPurchase:
        if not self._service:
            raise RuntimeError(self._config_error or "Google Play billing is not configured on the server")

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
            raise RuntimeError(self._config_error or "Google Play billing is not configured on the server")

        try:
            self._service.purchases().products().consume(
                packageName=self._package_name,
                productId=product_id,
                token=purchase_token,
            ).execute()
        except HttpError as exc:
            detail = _extract_http_error(exc)
            raise ValueError(f"Google Play consume failed: {detail}") from exc


@lru_cache
def get_google_play_verifier() -> GooglePlayVerifier:
    return GooglePlayVerifier()


def reload_google_play_verifier() -> GooglePlayVerifier:
    get_google_play_verifier.cache_clear()
    return get_google_play_verifier()


def _extract_http_error(exc: HttpError) -> str:
    try:
        payload = json.loads(exc.content.decode("utf-8"))
        return payload.get("error", {}).get("message", str(exc))
    except Exception:
        return str(exc)
