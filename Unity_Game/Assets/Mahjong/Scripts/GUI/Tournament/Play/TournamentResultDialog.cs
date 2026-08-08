using System;
using System.Collections;
using Mkey;
using Mkey.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mkey.Tournament
{
    public static class TournamentResultDialog
    {
        private const float AutoReturnSeconds = 3f;

        public static bool IsVisible => TournamentMessagePopup.IsVisible;

        public static void ShowDuelWin(int prizeCoins, Action onClosed) =>
            ShowDuelWin(prizeCoins, 1, onClosed);

        public static void ShowDuelWin(int prizeCoins, int rank, Action onClosed)
        {
            string rankLine = rank > 0 ? $"Rank #{rank}" : string.Empty;
            string body = string.IsNullOrEmpty(rankLine)
                ? $"Prize Coins +{prizeCoins:N0}"
                : $"Prize Coins +{prizeCoins:N0}\n{rankLine}";

            TournamentMessagePopup.Show(
                "YOU WIN",
                body,
                onClosed,
                autoCloseSeconds: AutoReturnSeconds);
        }

        public static void ShowDuelLoss(Action onClosed)
        {
            TournamentMessagePopup.Show(
                "YOU LOSE",
                "Better luck next time.",
                () =>
                {
                    if (Shell.MatchIQShellBridge.IsActive)
                    {
                        Shell.MatchIQShellBridge.ReturnMatchResult(false);
                        return;
                    }
                    onClosed?.Invoke();
                },
                autoCloseSeconds: AutoReturnSeconds);
        }

        public static void ShowRankWin(int rank, int prizeCoins, Action onClosed)
        {
            TournamentMessagePopup.Show(
                "YOU WIN",
                $"Prize Coins +{prizeCoins:N0}",
                onClosed,
                autoCloseSeconds: AutoReturnSeconds);
        }

        public static void ShowRankLoss(string tournamentId, int rank, Action onClosed)
        {
            TournamentMessagePopup.Show(
                "YOU LOSE",
                "Better luck next time.",
                () =>
                {
                    if (Shell.MatchIQShellBridge.IsActive)
                    {
                        Shell.MatchIQShellBridge.ReturnMatchResult(false);
                        return;
                    }
                    onClosed?.Invoke();
                },
                autoCloseSeconds: AutoReturnSeconds);
        }

        public static void ReturnToTournamentPage()
        {
            // If this match was started from the RN shell, hand result back instead of Unity lobby
            if (Shell.MatchIQShellBridge.IsActive)
            {
                Shell.MatchIQShellBridge.ReturnMatchResult(true);
                return;
            }

            TournamentFlowLog.ReturnToTournament("auto after result popup");
            TournamentRoomWebSocket.StopMaintainingConnection();
            TournamentApiBridge.Clear();
            TournamentMatchManager.DestroyRoom();
            TournamentSession.Clear();
            TournamentJoinCoordinator.NotifyWaitingRoomClosed();
            TournamentPageLifecycle.OnReturningFromMatch(RequestWalletRefreshOnReturn);

            // The Unity tournament page is no longer part of the build — exit to the shell.
            Shell.MatchIQShellBridge.ReturnToShell(false);
        }

        private static void RequestWalletRefreshOnReturn()
        {
            if (!NetworkManager.HasInstance)
                return;

            NetworkManager.Instance.StartCoroutine(SyncWalletOnReturnRoutine());
        }

        private static IEnumerator SyncWalletOnReturnRoutine()
        {
            var walletTask = WalletService.SyncToCoinsHolderAsync();
            while (!walletTask.IsCompleted)
                yield return null;
        }
    }
}
