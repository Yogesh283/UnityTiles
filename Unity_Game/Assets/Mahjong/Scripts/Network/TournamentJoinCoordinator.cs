using System;
using System.Collections;
using System.Threading.Tasks;
using Mkey;
using Mkey.Tournament;
using UnityEngine;

namespace Mkey.Network
{
    public static class TournamentJoinCoordinator
    {
        private const float BackgroundRetrySeconds = 2f;

        private static bool joinRequestInFlight;

        public static void NotifyWaitingRoomClosed()
        {
            joinRequestInFlight = false;
            TournamentApiBridge.SetBackgroundJoinActive(false);
        }

        /// <summary>
        /// Opens the full-screen waiting overlay immediately — no API calls.
        /// Safe to call multiple times.
        /// </summary>
        public static void OpenWaitingRoomImmediate(TournamentDefinition tournament)
        {
            if (tournament == null)
            {
                Debug.LogWarning("[WAITING_ROOM_OPEN] skipped — tournament is null");
                return;
            }

            TournamentFlowLog.WaitingRoomOpen($"tournament={tournament.id}");
            TournamentSession.Begin(tournament);
            TournamentRoomRegistry.JoinOrGetRoom(tournament);
            TournamentGlobalWaitingRoom.Show(tournament, TournamentGameBridge.LaunchGameFromWaitingRoom);
            TournamentFlowLog.WaitingRoomOpened(tournament.id);
            TournamentFlowLog.Searching("overlay shown before join API");
        }

        public static void ConfirmJoin(
            TournamentDefinition tournament,
            TournamentDialog dialog,
            TournamentWaitingRoomPanel waitingRoom,
            Action refreshWallet,
            Action<TournamentDefinition> retryJoin = null,
            Action onJoinFailed = null)
        {
            if (tournament == null) return;

            OpenWaitingRoomImmediate(tournament);

            if (ApiConfig.Current.UseLocalSimulation)
            {
                ConfirmJoinLocal(tournament, dialog, waitingRoom, refreshWallet, retryJoin, onJoinFailed);
                return;
            }

            StartBackgroundJoin(tournament, dialog, waitingRoom, refreshWallet, retryJoin, onJoinFailed);
        }

        public static void StartBackgroundJoin(
            TournamentDefinition tournament,
            TournamentDialog dialog,
            TournamentWaitingRoomPanel waitingRoom,
            Action refreshWallet,
            Action<TournamentDefinition> retryJoin = null,
            Action onJoinFailed = null)
        {
            if (tournament == null) return;

            if (joinRequestInFlight && TournamentGlobalWaitingRoom.IsVisible)
            {
                TournamentFlowLog.Join("background join already running — waiting room visible");
                return;
            }

            if (joinRequestInFlight && !TournamentGlobalWaitingRoom.IsVisible)
                joinRequestInFlight = false;

            if (TournamentJoinFlowGuard.IsRoomEstablished && TournamentApiBridge.HasMatchedRoom)
            {
                TournamentFlowLog.Join("room already established — skip background join");
                return;
            }

            joinRequestInFlight = true;
            TournamentFlowLog.JoinRequest($"tournament={tournament.id}");
            TournamentFlowLog.JoinApiStarted($"tournament={tournament.id}");
            TournamentApiBridge.SetBackgroundJoinActive(true);

            NetworkManager.EnsureExists();
            if (ApiConfig.Current.UseNakamaRealtimeNetworking)
            {
                NetworkManager.Instance.StartCoroutine(BackgroundJoinNakamaCoroutine(
                    tournament, dialog, waitingRoom, refreshWallet, retryJoin, onJoinFailed));
            }
            else
            {
                NetworkManager.Instance.StartCoroutine(BackgroundJoinOnlineCoroutine(
                    tournament, dialog, waitingRoom, refreshWallet, retryJoin, onJoinFailed));
            }
        }

        private static void AbortWaitingRoomOnFatalJoin(string reason)
        {
            TournamentFlowLog.RoomClosed(reason);
            TournamentGlobalWaitingRoom.Hide();
            TournamentApiBridge.Clear();
            TournamentSession.Clear();
            TournamentJoinFlowGuard.Reset();
            TournamentApiBridge.SetBackgroundJoinActive(false);
            joinRequestInFlight = false;
        }

        private static void ClearBackgroundJoinIfNotEstablished()
        {
            if (TournamentJoinFlowGuard.IsRoomEstablished)
                return;

            TournamentApiBridge.SetBackgroundJoinActive(false);
            joinRequestInFlight = false;
        }

        private static IEnumerator BackgroundJoinOnlineCoroutine(
            TournamentDefinition tournament,
            TournamentDialog dialog,
            TournamentWaitingRoomPanel waitingRoom,
            Action refreshWallet,
            Action<TournamentDefinition> retryJoin,
            Action onJoinFailed)
        {
            while (TournamentGlobalWaitingRoom.IsVisible && !TournamentJoinFlowGuard.IsRoomEstablished)
            {
                NetworkManager.Instance.StartCoroutine(FireAndForgetWalletSync(refreshWallet));

                Task<ApiResult<bool>> authTask = EnsureAuthenticatedAsync();
                while (!authTask.IsCompleted)
                    yield return null;

                ApiResult<bool> authResult = authTask.Result;
                if (!authResult.Success)
                {
                    TournamentFlowLog.JoinApiFailed(
                        $"auth status={authResult.StatusCode} err={authResult.ErrorMessage}");
                    TournamentFlowLog.ApiRetry(
                        $"auth status={authResult.StatusCode} err={authResult.ErrorMessage}");
                    yield return new WaitForSecondsRealtime(BackgroundRetrySeconds);
                    continue;
                }

                Task<ApiResult<RoomResponseDto>> joinTask = JoinTournamentOnceAsync(tournament.id);
                while (!joinTask.IsCompleted)
                    yield return null;

                ApiResult<RoomResponseDto> joinResult = joinTask.Result;
                if (joinResult.Success && joinResult.Data != null)
                {
                    ApplyJoinSuccess(tournament, joinResult.Data, refreshWallet);
                    yield break;
                }

                if (IsDefinitiveInsufficientBalance(joinResult))
                {
                    TournamentFlowLog.JoinApiFailed($"insufficient balance status={joinResult.StatusCode}");
                    HandleInsufficientBalance(
                        tournament, dialog, refreshWallet, retryJoin, onJoinFailed);
                    yield break;
                }

                if (joinResult.StatusCode == 401)
                    AuthService.Logout();

                TournamentFlowLog.JoinApiFailed(
                    $"status={joinResult.StatusCode} err={joinResult.ErrorMessage}");
                TournamentFlowLog.ApiRetry(
                    $"join status={joinResult.StatusCode} err={joinResult.ErrorMessage}");
                yield return new WaitForSecondsRealtime(BackgroundRetrySeconds);
            }

            ClearBackgroundJoinIfNotEstablished();
        }

        private static IEnumerator BackgroundJoinNakamaCoroutine(
            TournamentDefinition tournament,
            TournamentDialog dialog,
            TournamentWaitingRoomPanel waitingRoom,
            Action refreshWallet,
            Action<TournamentDefinition> retryJoin,
            Action onJoinFailed)
        {
            RoomResponseDto apiRoom = null;

            while (TournamentGlobalWaitingRoom.IsVisible && !TournamentJoinFlowGuard.IsRoomEstablished)
            {
                NetworkManager.Instance.StartCoroutine(FireAndForgetWalletSync(refreshWallet));

                Task<ApiResult<bool>> authTask = EnsureAuthenticatedAsync();
                while (!authTask.IsCompleted)
                    yield return null;

                ApiResult<bool> authResult = authTask.Result;
                if (!authResult.Success)
                {
                    TournamentFlowLog.JoinApiFailed(
                        $"auth status={authResult.StatusCode} err={authResult.ErrorMessage}");
                    yield return new WaitForSecondsRealtime(BackgroundRetrySeconds);
                    continue;
                }

                if (apiRoom == null)
                {
                    Task<ApiResult<RoomResponseDto>> joinTask = JoinTournamentOnceAsync(tournament.id);
                    while (!joinTask.IsCompleted)
                        yield return null;

                    ApiResult<RoomResponseDto> joinResult = joinTask.Result;
                    if (!joinResult.Success || joinResult.Data == null)
                    {
                        if (IsDefinitiveInsufficientBalance(joinResult))
                        {
                            TournamentFlowLog.JoinApiFailed($"insufficient balance status={joinResult.StatusCode}");
                            HandleInsufficientBalance(
                                tournament, dialog, refreshWallet, retryJoin, onJoinFailed);
                            yield break;
                        }

                        if (joinResult.StatusCode == 401)
                            AuthService.Logout();

                        TournamentFlowLog.JoinApiFailed(
                            $"status={joinResult.StatusCode} err={joinResult.ErrorMessage}");
                        yield return new WaitForSecondsRealtime(BackgroundRetrySeconds);
                        continue;
                    }

                    apiRoom = joinResult.Data;
                    TournamentApiBridge.ApplyJoinResponse(tournament, apiRoom);
                    if (apiRoom.playerCount >= 2)
                        TournamentFlowLog.PlayerFound($"players={apiRoom.playerCount}/{tournament.maxPlayers}");
                    NetworkManager.Instance.StartCoroutine(FinishJoinConnectivityCoroutine(apiRoom));
                }

                Task<ApiResult<RoomResponseDto>> nakamaTask =
                    NakamaTournamentRealtimeClient.MatchmakeAndJoinAsync(tournament, apiRoom);
                while (!nakamaTask.IsCompleted)
                    yield return null;

                ApiResult<RoomResponseDto> nakamaResult = nakamaTask.Result;
                if (nakamaResult.Success && nakamaResult.Data != null)
                {
                    RoomResponseDto mergedRoom = MergeNakamaWithBusinessRoom(apiRoom, nakamaResult.Data);
                    ApplyJoinSuccess(tournament, mergedRoom, refreshWallet);
                    yield break;
                }

                TournamentFlowLog.JoinApiFailed(
                    $"nakama status={nakamaResult.StatusCode} err={nakamaResult.ErrorMessage}");
                yield return new WaitForSecondsRealtime(BackgroundRetrySeconds);
            }

            ClearBackgroundJoinIfNotEstablished();
        }

        private static void ApplyJoinSuccess(
            TournamentDefinition tournament,
            RoomResponseDto room,
            Action refreshWallet)
        {
            TournamentFlowLog.JoinApiSuccess(
                $"room={room.roomId} status={room.status} players={room.playerCount}");
            TournamentFlowLog.JoinResponseParsed(
                room.roomId,
                room.status,
                room.playerCount,
                room.matchStartAtMs);
            TournamentTransitionProbe.Step(1, "Server room status on JOIN",
                room.status is "starting" or "active" or "waiting",
                $"status={room.status} players={room.playerCount}/{room.maxPlayers}");
            TournamentTransitionProbe.Step(2, "match_start_at_ms on JOIN",
                (room.matchStartAtMs ?? 0) > 0,
                room.matchStartAtMs?.ToString() ?? "null");
            TournamentFlowLog.RoomCreated(room.roomId);
            TournamentFlowLog.RoomId(room.roomId);
            if (room.playerCount >= 2)
                TournamentFlowLog.RoomFull($"players={room.playerCount}");

            TournamentApiBridge.ApplyJoinResponse(tournament, room);
            TournamentFlowLog.JoinSuccess(
                $"room={room.roomId} status={room.status} players={room.playerCount}");

            ApplyWalletFromJoinResponse(room);
            refreshWallet?.Invoke();
            TournamentJoinFlowGuard.MarkRoomEstablished();
            TournamentApiBridge.SetBackgroundJoinActive(false);
            joinRequestInFlight = false;

            if (room.walletBalance.HasValue)
                TournamentFlowLog.WalletSynced(room.walletBalance.Value);

            NetworkManager.Instance.StartCoroutine(FinishJoinConnectivityCoroutine(room));

            if (!room.walletBalance.HasValue)
                NetworkManager.Instance.StartCoroutine(FireAndForgetWalletSync(refreshWallet));
        }

        private static IEnumerator FinishJoinConnectivityCoroutine(RoomResponseDto room)
        {
            if (room == null || string.IsNullOrEmpty(room.roomId))
                yield break;

            if (!ApiConfig.Current.UseNakamaRealtimeNetworking)
            {
                TournamentFlowLog.WsConnecting($"room={room.roomId}");
                TournamentFlowLog.ConnectingWebSocket(room.roomId);
                Task<bool> wsTask = TournamentRoomWebSocket.ConnectAndWaitAsync(room.roomId, timeoutMs: 15000);
                while (!wsTask.IsCompleted)
                    yield return null;

                if (wsTask.Result)
                    TournamentFlowLog.WsConnected($"room={room.roomId}");
                else
                    TournamentFlowLog.ApiRetry($"ws connect failed room={room.roomId}");
            }

            Task<bool> refreshTask = TournamentApiBridge.RefreshActiveRoomAsync();
            while (!refreshTask.IsCompleted)
                yield return null;
        }

        private static IEnumerator FireAndForgetWalletSync(Action refreshWallet)
        {
            Task<ApiResult<int>> walletTask = WalletService.SyncToCoinsHolderAsync();
            while (!walletTask.IsCompleted)
                yield return null;

            if (walletTask.Result.Success)
                refreshWallet?.Invoke();
        }

        private static void HandleInsufficientBalance(
            TournamentDefinition tournament,
            TournamentDialog dialog,
            Action refreshWallet,
            Action<TournamentDefinition> retryJoin,
            Action onJoinFailed)
        {
            AbortWaitingRoomOnFatalJoin("insufficient balance");
            onJoinFailed?.Invoke();
            int balance = CoinsHolder.Instance ? CoinsHolder.Count : 0;
            dialog.ShowInsufficientCoins(
                tournament.entryFee,
                balance,
                () => OpenDeposit(dialog, refreshWallet, () => retryJoin?.Invoke(tournament)),
                onJoinFailed);
        }

        private static async Task<ApiResult<RoomResponseDto>> JoinTournamentOnceAsync(string tournamentId)
        {
            TournamentFlowLog.Join($"API POST tournaments/join tournament_id={tournamentId}");
            return await TournamentService.JoinTournamentAsync(tournamentId);
        }

        private static async Task<ApiResult<bool>> EnsureAuthenticatedAsync()
        {
            if (NetworkManager.Instance.IsAuthenticated)
            {
                var session = await NetworkManager.Instance.GetAsync<UserProfileDto>("auth/me");
                if (session.Success)
                    return ApiResult<bool>.Ok(true);

                AuthService.Logout();
            }

            var login = await GuestLoginWithRetryAsync();
            if (!login.Success)
                return ApiResult<bool>.Fail(
                    login.ErrorMessage,
                    login.StatusCode,
                    login.IsServerUnavailable);

            return ApiResult<bool>.Ok(true);
        }

        private static async Task<ApiResult<TokenResponseDto>> GuestLoginWithRetryAsync()
        {
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var login = await AuthService.GuestLoginAsync();
                if (login.Success)
                    return login;

                if (!login.IsServerUnavailable && login.StatusCode != 0)
                    return login;

                if (attempt < maxAttempts)
                    await Task.Delay(attempt * 1000);
            }

            return await AuthService.GuestLoginAsync();
        }

        private static void ConfirmJoinLocal(
            TournamentDefinition tournament,
            TournamentDialog dialog,
            TournamentWaitingRoomPanel waitingRoom,
            Action refreshWallet,
            Action<TournamentDefinition> retryJoin,
            Action onJoinFailed)
        {
            if (!CoinsHolder.Instance)
            {
                dialog.Show("Error", "Coins system not ready. Please return to the map and try again.", false, null, null);
                onJoinFailed?.Invoke();
                return;
            }

            if (CoinsHolder.Count < tournament.entryFee)
            {
                int balance = CoinsHolder.Count;
                dialog.ShowInsufficientCoins(
                    tournament.entryFee,
                    balance,
                    () => OpenDeposit(dialog, refreshWallet, () => retryJoin?.Invoke(tournament)),
                    onJoinFailed);
                return;
            }

            CoinsHolder.Add(-tournament.entryFee);
            refreshWallet?.Invoke();
            TournamentJoinFlowGuard.MarkRoomEstablished();
        }

        private static void OpenDeposit(
            TournamentDialog dialog,
            Action refreshWallet,
            Action onComplete)
        {
            if (!dialog)
                return;

            dialog.ShowDepositMenu(() =>
            {
                refreshWallet?.Invoke();
                onComplete?.Invoke();
            });
        }

        private static bool IsDefinitiveInsufficientBalance<T>(ApiResult<T> result) =>
            result.StatusCode == 400 && IsInsufficientBalance(result.ErrorMessage);

        private static bool IsInsufficientBalance(string message) =>
            !string.IsNullOrEmpty(message) &&
            message.IndexOf("insufficient", StringComparison.OrdinalIgnoreCase) >= 0;

        private static void ApplyWalletFromJoinResponse(RoomResponseDto room)
        {
            if (room == null || !room.walletBalance.HasValue || !CoinsHolder.Instance)
                return;

            CoinsHolder.Instance.SetCount(room.walletBalance.Value);
            WalletService.CachedBalance = room.walletBalance.Value;
        }

        private static RoomResponseDto MergeNakamaWithBusinessRoom(RoomResponseDto business, RoomResponseDto nakama)
        {
            if (business == null)
                return nakama;

            if (nakama == null)
                return business;

            business.status = string.IsNullOrEmpty(nakama.status) ? business.status : nakama.status;
            business.playerCount = Mathf.Max(business.playerCount, nakama.playerCount);
            if ((nakama.matchStartAtMs ?? 0) > 0)
                business.matchStartAtMs = nakama.matchStartAtMs;
            if (nakama.startCountdownSeconds.HasValue)
                business.startCountdownSeconds = nakama.startCountdownSeconds;
            if ((nakama.serverNowMs ?? 0) > 0)
                business.serverNowMs = nakama.serverNowMs;
            if (!string.IsNullOrEmpty(nakama.searchStatus))
                business.searchStatus = nakama.searchStatus;
            if (nakama.players != null && nakama.players.Count > 0)
                business.players = nakama.players;

            return business;
        }
    }
}
