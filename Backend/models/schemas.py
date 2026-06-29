from datetime import datetime
from typing import Any

from pydantic import BaseModel, EmailStr, Field


class TokenResponse(BaseModel):
    access_token: str
    token_type: str = "bearer"
    user_id: int
    user_uuid: str
    display_name: str


class RegisterRequest(BaseModel):
    email: EmailStr
    password: str = Field(min_length=6)
    display_name: str = "Player"


class LoginRequest(BaseModel):
    email: EmailStr
    password: str


class GuestLoginRequest(BaseModel):
    guest_id: str
    display_name: str = "Guest"


class GoogleLoginRequest(BaseModel):
    google_id: str
    email: EmailStr
    display_name: str


class WalletResponse(BaseModel):
    balance: int


class IapProductResponse(BaseModel):
    product_id: str
    coins: int
    price_inr: int
    display_name: str


class GooglePlayVerifyRequest(BaseModel):
    product_id: str = Field(min_length=1, max_length=128)
    purchase_token: str = Field(min_length=1, max_length=512)


class GooglePlayVerifyResponse(BaseModel):
    already_processed: bool
    order_id: str
    product_id: str
    coins_added: int
    balance: int


class TournamentResponse(BaseModel):
    id: str
    icon: str
    display_name: str
    max_players: int
    entry_fee: int
    prize_pool: int
    platform_fee: int
    reward_info: str
    waiting_seconds: int
    status_label: str


class JoinTournamentRequest(BaseModel):
    tournament_id: str


class RoomResponse(BaseModel):
    room_id: str
    tournament_id: str
    level_index: int
    level_seed: int
    status: str
    player_count: int
    max_players: int
    waiting_seconds: int


class SubmitScoreRequest(BaseModel):
    room_id: str
    score: int
    moves: int
    elapsed_seconds: int


class SubmitScoreResponse(BaseModel):
    ok: bool = True
    finalized: bool = False
    rank: int | None = None
    prize: int = 0
    room_status: str = "active"


class TournamentLevelRewardRequest(BaseModel):
    room_id: str = Field(min_length=1, max_length=128)


class LevelCompleteRequest(BaseModel):
    user_uuid: str = Field(min_length=1, max_length=36)
    level_number: int = Field(ge=1, le=300)


class LevelCompleteResponse(BaseModel):
    reward_given: bool
    reward_coins: int
    current_wallet_balance: int


class LeaderboardEntryResponse(BaseModel):
    user_id: int
    display_name: str
    total_wins: int
    total_prize: int
    tournaments_played: int
    best_rank: int


class NotificationResponse(BaseModel):
    id: int
    title: str
    body: str
    is_read: bool
    created_at: datetime
