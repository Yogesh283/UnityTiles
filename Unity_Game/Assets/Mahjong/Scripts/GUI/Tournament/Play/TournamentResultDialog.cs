using System;
using System.Collections;
using System.Collections.Generic;
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

        public static void ShowDuelWin(int prizeCoins, Action onClosed)
        {
            TournamentMessagePopup.Show(
                "YOU WIN",
                $"Prize Coins +{prizeCoins:N0}",
                onClosed,
                autoCloseSeconds: AutoReturnSeconds);
        }

        public static void ShowDuelLoss(Action onClosed)
        {
            TournamentMessagePopup.Show(
                "YOU LOSE",
                string.Empty,
                onClosed,
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
                string.Empty,
                onClosed,
                autoCloseSeconds: AutoReturnSeconds);
        }

        public static void ReturnToTournamentPage()
        {
            TournamentMatchManager.DestroyRoom();
            TournamentSession.Clear();
            TournamentPageLifecycle.OnReturningFromMatch(RequestWalletRefreshOnReturn);

            if (SceneLoader.Instance)
                SceneLoader.Instance.LoadScene(TournamentSession.TournamentSceneIndex);
            else
                SceneManager.LoadScene(TournamentSession.TournamentSceneIndex);
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
