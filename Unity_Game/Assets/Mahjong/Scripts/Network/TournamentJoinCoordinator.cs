using System;
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
                ConfirmJoinLocal(tournament, dialog, waitingRoom, refreshWallet, retryJoin);
                return;
            }

            try
            {
                var joinResult = await TournamentService.JoinTournamentAsync(tournament.id);
                if (!joinResult.Success || joinResult.Data == null)
                {
                    ShowServerError(dialog, tournament, dialogRef => ConfirmJoin(
                        tournament, dialogRef, waitingRoom, refreshWallet, retryJoin, onJoinFailed), onJoinFailed);
                    return;
                }

                var walletResult = await WalletService.SyncToCoinsHolderAsync();
                refreshWallet?.Invoke();

                if (!walletResult.Success)
                    Debug.LogWarning("[TournamentJoin] Wallet sync failed after join: " + walletResult.ErrorMessage);

                TournamentApiBridge.ApplyJoinResponse(tournament, joinResult.Data);
                waitingRoom.transform.SetAsLastSibling();
                TournamentSession.Begin(tournament);
                waitingRoom.Show(tournament, TournamentGameBridge.LaunchGameFromWaitingRoom);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                ShowServerError(dialog, tournament, dialogRef => ConfirmJoin(
                    tournament, dialogRef, waitingRoom, refreshWallet, retryJoin, onJoinFailed), onJoinFailed);
            }
        }

        private static void ConfirmJoinLocal(
            TournamentDefinition tournament,
            TournamentDialog dialog,
            TournamentWaitingRoomPanel waitingRoom,
            Action refreshWallet,
            Action<TournamentDefinition> retryJoin)
        {
            if (!CoinsHolder.Instance)
            {
                dialog.Show("Error", "Coins system not ready. Please return to the map and try again.", false, null, null);
                return;
            }

            if (CoinsHolder.Count < tournament.entryFee)
            {
                int balance = CoinsHolder.Count;
                dialog.ShowInsufficientCoins(
                    tournament.entryFee,
                    balance,
                    () => OpenDeposit(dialog, refreshWallet, () => retryJoin?.Invoke(tournament)),
                    null);
                return;
            }

            CoinsHolder.Add(-tournament.entryFee);
            refreshWallet?.Invoke();
            waitingRoom.transform.SetAsLastSibling();
            TournamentSession.Begin(tournament);
            TournamentRoomRegistry.JoinOrGetRoom(tournament);
            waitingRoom.Show(tournament, TournamentGameBridge.LaunchGameFromWaitingRoom);
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

        private static void ShowServerError(
            TournamentDialog dialog,
            TournamentDefinition tournament,
            Action<TournamentDialog> retry,
            Action onFailed)
        {
            dialog.Show(
                NetworkManager.ServerUnavailableMessage,
                "Could not join the tournament.\nPlease check your connection and try again.",
                true,
                () => retry?.Invoke(dialog),
                onFailed);

            onFailed?.Invoke();
        }
    }
}
