using System;
using System.Threading.Tasks;
using Mkey;
using Mkey.Tournament;
using UnityEngine;

namespace Mkey.Network
{
    public static class TournamentJoinCoordinator
    {
        public static async void ConfirmJoin(
            TournamentDefinition tournament,
            TournamentDialog dialog,
            TournamentWaitingRoomPanel waitingRoom,
            Action refreshWallet,
            Action<TournamentDefinition> retryJoin = null,
            Action onJoinFailed = null)
        {
            if (tournament == null) return;

            if (ApiConfig.Current.UseLocalSimulation)
            {
                ConfirmJoinLocal(tournament, dialog, waitingRoom, refreshWallet, retryJoin, onJoinFailed);
                return;
            }

            try
            {
                ApiResult<bool> authResult = await EnsureAuthenticatedAsync();
                if (!authResult.Success)
                {
                    ShowJoinError(
                        dialog,
                        tournament,
                        authResult.ErrorMessage,
                        authResult.StatusCode,
                        authResult.IsServerUnavailable,
                        dialogRef => ConfirmJoin(
                            tournament, dialogRef, waitingRoom, refreshWallet, retryJoin, onJoinFailed),
                        refreshWallet,
                        retryJoin,
                        onJoinFailed);
                    return;
                }

                var walletResult = await WalletService.SyncToCoinsHolderAsync();
                refreshWallet?.Invoke();

                if (walletResult.Success &&
                    CoinsHolder.Instance &&
                    CoinsHolder.Count < tournament.entryFee)
                {
                    onJoinFailed?.Invoke();
                    dialog.ShowInsufficientCoins(
                        tournament.entryFee,
                        CoinsHolder.Count,
                        () => OpenDeposit(dialog, refreshWallet, () => retryJoin?.Invoke(tournament)),
                        onJoinFailed);
                    return;
                }

                var joinResult = await TournamentService.JoinTournamentAsync(tournament.id);
                if (!joinResult.Success || joinResult.Data == null)
                {
                    if (joinResult.StatusCode == 401)
                    {
                        AuthService.Logout();
                        authResult = await EnsureAuthenticatedAsync();
                        if (authResult.Success)
                            joinResult = await TournamentService.JoinTournamentAsync(tournament.id);
                    }
                    else if (joinResult.IsServerUnavailable)
                    {
                        await Task.Delay(750);
                        joinResult = await TournamentService.JoinTournamentAsync(tournament.id);
                    }
                }

                if (!joinResult.Success || joinResult.Data == null)
                {
                    ShowJoinError(
                        dialog,
                        tournament,
                        joinResult.ErrorMessage,
                        joinResult.StatusCode,
                        joinResult.IsServerUnavailable,
                        dialogRef => ConfirmJoin(
                            tournament, dialogRef, waitingRoom, refreshWallet, retryJoin, onJoinFailed),
                        refreshWallet,
                        retryJoin,
                        onJoinFailed);
                    return;
                }

                TournamentApiBridge.ApplyJoinResponse(tournament, joinResult.Data);
                TournamentSession.Begin(tournament);
                TournamentJoinFlowGuard.Reset();
                TournamentGlobalWaitingRoom.Show(tournament, TournamentGameBridge.LaunchGameFromWaitingRoom);

                ApplyWalletFromJoinResponse(joinResult.Data);
                refreshWallet?.Invoke();
                await WalletService.SyncToCoinsHolderAsync();
                refreshWallet?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                ShowJoinError(
                    dialog,
                    tournament,
                    ex.Message,
                    0,
                    true,
                    dialogRef => ConfirmJoin(
                        tournament, dialogRef, waitingRoom, refreshWallet, retryJoin, onJoinFailed),
                    refreshWallet,
                    retryJoin,
                    onJoinFailed);
            }
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
            TournamentSession.Begin(tournament);
            TournamentRoomRegistry.JoinOrGetRoom(tournament);
            TournamentJoinFlowGuard.Reset();
            TournamentGlobalWaitingRoom.Show(tournament, TournamentGameBridge.LaunchGameFromWaitingRoom);
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

        private static void ShowJoinError(
            TournamentDialog dialog,
            TournamentDefinition tournament,
            string errorMessage,
            int statusCode,
            bool serverUnavailable,
            Action<TournamentDialog> retry,
            Action refreshWallet,
            Action<TournamentDefinition> retryJoin,
            Action onFailed)
        {
            string detail = string.IsNullOrWhiteSpace(errorMessage) ? "Unknown error." : errorMessage;

            if (IsInsufficientBalance(detail))
            {
                int balance = CoinsHolder.Instance ? CoinsHolder.Count : 0;
                dialog.ShowInsufficientCoins(
                    tournament.entryFee,
                    balance,
                    () => OpenDeposit(dialog, refreshWallet, () => retryJoin?.Invoke(tournament)),
                    onFailed);
                onFailed?.Invoke();
                return;
            }

            string title;
            string message;

            if (statusCode >= 500)
            {
                title = "Server Error";
                message = "The game server had a problem.\nPlease try again in a moment.";
            }
            else if (serverUnavailable)
            {
                // Keep join flow non-blocking on transient outages.
                Debug.LogWarning("[TournamentJoin] Server temporarily unavailable; skipping blocking popup.");
                onFailed?.Invoke();
                return;
            }
            else if (statusCode == 0)
            {
                title = "Could Not Join";
                message = detail;
            }
            else if (statusCode == 401 || detail.IndexOf("authenticated", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                title = "Sign In Required";
                message = "Your session expired.\nPlease try again.";
            }
            else
            {
                title = "Could Not Join";
                message = detail;
            }

            Debug.LogWarning($"[TournamentJoin] Failed ({statusCode}): {detail}");

            dialog.Show(
                title,
                message,
                true,
                () => retry?.Invoke(dialog),
                onFailed);

            onFailed?.Invoke();
        }

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
    }
}
