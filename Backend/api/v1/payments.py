from fastapi import APIRouter, Depends, Header, HTTPException, Request, status
from sqlalchemy.orm import Session

from auth.jwt import get_current_user
from config import get_settings
from database.connection import get_db
from database.models import User
from models.schemas import (
    DepositCreateRequest,
    GooglePlayVerifyRequest,
    GooglePlayVerifyResponse,
    IapProductResponse,
    RazorpayVerifyRequest,
)
from payments.catalog import IAP_PRODUCTS
from payments.deposit import DepositService
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
        "active": verifier.is_configured,
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


@router.get("/deposit/methods")
def deposit_methods(db: Session = Depends(get_db)):
    return DepositService(db).methods()


@router.post("/deposit/create")
def create_deposit(
    body: DepositCreateRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    try:
        return DepositService(db).create(user, body.method, body.amount)
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(exc)) from exc


@router.post("/deposit/razorpay/verify")
def verify_razorpay_deposit(
    body: RazorpayVerifyRequest,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    try:
        return DepositService(db).verify_razorpay(
            user,
            body.razorpay_order_id,
            body.razorpay_payment_id,
            body.razorpay_signature,
        )
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(exc)) from exc


@router.get("/deposit/{order_code}")
def deposit_status(
    order_code: str,
    user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    try:
        return DepositService(db).get_order(user.id, order_code)
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail=str(exc)) from exc


@router.post("/webhooks/razorpay")
async def razorpay_webhook(
    request: Request,
    db: Session = Depends(get_db),
    x_razorpay_signature: str | None = Header(default=None),
):
    body = await request.body()
    try:
        return DepositService(db).handle_razorpay_webhook(body, x_razorpay_signature or "")
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(exc)) from exc


@router.post("/webhooks/nowpayments")
async def nowpayments_webhook(
    request: Request,
    db: Session = Depends(get_db),
    x_nowpayments_sig: str | None = Header(default=None),
):
    payload = await request.json()
    try:
        return DepositService(db).handle_nowpayments_ipn(payload, x_nowpayments_sig or "")
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(exc)) from exc
