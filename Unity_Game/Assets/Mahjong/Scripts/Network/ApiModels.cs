using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mkey.Network
{
    [Serializable]
    public class TokenResponseDto
    {
        [JsonProperty("access_token")] public string accessToken;
        [JsonProperty("token_type")] public string tokenType;
        [JsonProperty("user_id")] public int userId;
        [JsonProperty("user_uuid")] public string userUuid;
        [JsonProperty("display_name")] public string displayName;
    }

    [Serializable]
    public class RegisterRequestDto
    {
        public string email;
        public string password;
        [JsonProperty("display_name")] public string displayName;
    }

    [Serializable]
    public class LoginRequestDto
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class GuestLoginRequestDto
    {
        [JsonProperty("guest_id")] public string guestId;
        [JsonProperty("display_name")] public string displayName;
    }

    [Serializable]
    public class WalletBalanceDto
    {
        public int balance;
    }

    [Serializable]
    public class WalletTransactionDto
    {
        public long id;
        public int amount;
        [JsonProperty("balance_after")] public int balanceAfter;
        public string type;
        [JsonProperty("reference_id")] public string referenceId;
        public string note;
        [JsonProperty("created_at")] public string createdAt;
    }

    [Serializable]
    public class TournamentDto
    {
        public string id;
        public string icon;
        [JsonProperty("display_name")] public string displayName;
        [JsonProperty("max_players")] public int maxPlayers;
        [JsonProperty("entry_fee")] public int entryFee;
        [JsonProperty("prize_pool")] public int prizePool;
        [JsonProperty("platform_fee")] public int platformFee;
        [JsonProperty("reward_info")] public string rewardInfo;
        [JsonProperty("waiting_seconds")] public int waitingSeconds;
        [JsonProperty("status_label")] public string statusLabel;
    }

    [Serializable]
    public class JoinTournamentRequestDto
    {
        [JsonProperty("tournament_id")] public string tournamentId;
    }

    [Serializable]
    public class RoomResponseDto
    {
        [JsonProperty("room_id")] public string roomId;
        [JsonProperty("tournament_id")] public string tournamentId;
        [JsonProperty("tournament_name")] public string tournamentName;
        [JsonProperty("level_index")] public int levelIndex;
        [JsonProperty("level_seed")] public int levelSeed;
        public string status;
        [JsonProperty("player_count")] public int playerCount;
        [JsonProperty("max_players")] public int maxPlayers;
        [JsonProperty("waiting_seconds")] public int waitingSeconds;
        [JsonProperty("waiting_seconds_remaining")] public int waitingSecondsRemaining;
        [JsonProperty("start_countdown_seconds")] public int startCountdownSeconds;
        [JsonProperty("match_start_at_ms")] public long matchStartAtMs;
        [JsonProperty("server_now_ms")] public long serverNowMs;
        [JsonProperty("search_status")] public string searchStatus;
        public bool queued;
        public List<RoomPlayerDto> players;
    }

    [Serializable]
    public class RoomPlayerDto
    {
        [JsonProperty("user_id")] public int userId;
        [JsonProperty("user_uuid")] public string userUuid;
        [JsonProperty("display_name")] public string displayName;
        [JsonProperty("avatar_url")] public string avatarUrl;
        [JsonProperty("current_rank")] public int currentRank;
        [JsonProperty("tournament_id")] public string tournamentId;
        public int score;
        public int moves;
        [JsonProperty("elapsed_seconds")] public int elapsedSeconds;
        public int rank;
        [JsonProperty("is_connected")] public bool isConnected;
        [JsonProperty("has_submitted")] public bool hasSubmitted;
    }

    [Serializable]
    public class RoomSnapshotDto
    {
        [JsonProperty("room_id")] public string roomId;
        [JsonProperty("tournament_id")] public string tournamentId;
        [JsonProperty("level_index")] public int levelIndex;
        public string status;
        [JsonProperty("paid_winner_slots")] public int paidWinnerSlots;
        [JsonProperty("submitted_count")] public int submittedCount;
        public List<RoomPlayerDto> players;
    }

    [Serializable]
    public class SubmitScoreRequestDto
    {
        [JsonProperty("room_id")] public string roomId;
        public int score;
        public int moves;
        [JsonProperty("elapsed_seconds")] public int elapsedSeconds;
    }

    [Serializable]
    public class SubmitScoreResponseDto
    {
        public bool ok;
        public bool finalized;
        public int rank;
        public int prize;
        [JsonProperty("room_status")] public string roomStatus;
    }

    [Serializable]
    public class TournamentLevelRewardRequestDto
    {
        [JsonProperty("room_id")] public string roomId;
    }

    [Serializable]
    public class LevelCompleteRequestDto
    {
        [JsonProperty("user_uuid")] public string userUuid;
        [JsonProperty("level_number")] public int levelNumber;
    }

    [Serializable]
    public class LevelCompleteResponseDto
    {
        [JsonProperty("reward_given")] public bool rewardGiven;
        [JsonProperty("reward_coins")] public int rewardCoins;
        [JsonProperty("current_wallet_balance")] public int currentWalletBalance;
    }

    [Serializable]
    public class TournamentHistoryDto
    {
        [JsonProperty("tournament_id")] public string tournamentId;
        [JsonProperty("room_id")] public string roomId;
        public int rank;
        public int score;
        public int prize;
        [JsonProperty("created_at")] public string createdAt;
    }

    [Serializable]
    public class LeaderboardEntryDto
    {
        [JsonProperty("user_id")] public int userId;
        [JsonProperty("display_name")] public string displayName;
        [JsonProperty("total_wins")] public int totalWins;
        [JsonProperty("total_prize")] public int totalPrize;
        [JsonProperty("tournaments_played")] public int tournamentsPlayed;
        [JsonProperty("best_rank")] public int bestRank;
    }

    [Serializable]
    public class UserProfileDto
    {
        public int id;
        public string email;
        [JsonProperty("display_name")] public string displayName;
        [JsonProperty("is_guest")] public bool isGuest;
        [JsonProperty("avatar_url")] public string avatarUrl;
    }

    [Serializable]
    public class OkResponseDto
    {
        public bool ok;
    }

    [Serializable]
    public class IapProductDto
    {
        [JsonProperty("product_id")] public string productId;
        public int coins;
        [JsonProperty("price_inr")] public int priceInr;
        [JsonProperty("display_name")] public string displayName;
    }

    [Serializable]
    public class GooglePlayVerifyRequestDto
    {
        [JsonProperty("product_id")] public string productId;
        [JsonProperty("purchase_token")] public string purchaseToken;
    }

    [Serializable]
    public class GooglePlayVerifyResponseDto
    {
        [JsonProperty("already_processed")] public bool alreadyProcessed;
        [JsonProperty("order_id")] public string orderId;
        [JsonProperty("product_id")] public string productId;
        [JsonProperty("coins_added")] public int coinsAdded;
        public int balance;
    }

    [Serializable]
    public class GooglePlayBillingStatusDto
    {
        public bool active;
        public bool configured;
        [JsonProperty("package_name")] public string packageName;
        public string error;
    }
}
