from fastapi import APIRouter

from api.v1 import auth, leaderboard, notifications, payments, presence, tournament, wallet

router = APIRouter(prefix="/api/v1")
router.include_router(auth.router)
router.include_router(wallet.router)
router.include_router(payments.router)
router.include_router(tournament.router)
router.include_router(leaderboard.router)
router.include_router(notifications.router)
router.include_router(presence.router)
