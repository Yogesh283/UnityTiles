from fastapi import APIRouter, Depends, HTTPException, status
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from config import get_settings
from database.connection import get_db
from database.models import User
from models.schemas import (
    GooglePlayVerifyRequest,
    GooglePlayVerifyResponse,
    IapProductResponse,
)
from payments.catalog import IAP_PRODUCTS
from payments.google_play import get_google_play_verifier
from payments.service import PaymentService

router = APIRouter(prefix="/payments", tags=["payments"])


@router.get("/products", response_model=list[IapProductResponse])
def list_products():
    return [
        IapProductResponse(
            product_id=product.product_id,
            coins=product.coins,
            price_inr=product.price_inr,
            display_name=product.display_name,
        )
        for product in IAP_PRODUCTS.values()
    ]


@router.get("/google/status")
def google_play_status():
    settings = get_settings()
    verifier = get_google_play_verifier()
    return {
        "configured": verifier.is_configured,
        "package_name": settings.google_play_package_name,
        "error": verifier.config_error,
    }


@router.post("/google/verify", response_model=GooglePlayVerifyResponse)
def verify_google_play(
    body: GooglePlayVerifyRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    verifier = get_google_play_verifier()
    if not verifier.is_configured:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail=verifier.config_error or "Google Play billing is not configured on the server",
        )

    try:
        result = PaymentService(db).verify_google_play_purchase(
            user_id=user.id,
            product_id=body.product_id,
            purchase_token=body.purchase_token,
        )
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(exc)) from exc

    return GooglePlayVerifyResponse(**result)
