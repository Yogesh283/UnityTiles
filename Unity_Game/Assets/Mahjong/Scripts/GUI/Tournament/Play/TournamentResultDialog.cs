using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mkey.Tournament
{
    public static class TournamentResultDialog
    {
        private const float AutoReturnSeconds = 3f;

        public static bool IsVisible => TournamentPremiumOverlay.IsVisible;

        private static string TournamentSubtitle()
        {
            TournamentDefinition tournament = TournamentSession.Tournament;
            if (tournament == null) return string.Empty;
            return $"{tournament.icon} {tournament.displayName}";
        }

        public static void ShowDuelWin(int prizeCoins, Action onClosed)
        {
            TournamentPremiumOverlay.Show(
                "YOU WIN!",
                TournamentSubtitle(),
                $"+{prizeCoins:N0} COINS",
                "Congratulations!\n\nRank: #1",
                footer: string.Empty,
                onClosed: onClosed,
                autoReturnSeconds: AutoReturnSeconds);
        }

        public static void ShowDuelLoss(Action onClosed)
        {
            TournamentPremiumOverlay.Show(
                "YOU LOSE",
                TournamentSubtitle(),
                "GAME OVER",
                "Your opponent completed the level first.\n\nBetter luck next time.",
                footer: "Reward: 0 Coins",
                onClosed: onClosed,
                autoReturnSeconds: AutoReturnSeconds);
        }

        public static void ShowRankWin(int rank, int prizeCoins, Action onClosed)
        {
            TournamentPremiumOverlay.Show(
                "YOU WIN!",
                TournamentSubtitle(),
                $"+{prizeCoins:N0} COINS",
                $"Congratulations!\n\nRank: #{rank:N0}",
                footer: string.Empty,
                onClosed: onClosed,
                autoReturnSeconds: AutoReturnSeconds);
        }

        public static void ShowRankLoss(string tournamentId, int rank, Action onClosed)
        {
            TournamentPremiumOverlay.Show(
                "YOU LOSE",
                TournamentSubtitle(),
                "GAME OVER",
                $"Better Luck Next Time\n\nRank: #{rank:N0}",
                footer: "Reward: 0 Coins",
                onClosed: onClosed,
                autoReturnSeconds: AutoReturnSeconds);
        }

        public static void ReturnToTournamentPage()
        {
            TournamentMatchManager.DestroyRoom();
            TournamentSession.Clear();

            if (SceneLoader.Instance)
                SceneLoader.Instance.LoadScene(TournamentSession.TournamentSceneIndex);
            else
                SceneManager.LoadScene(TournamentSession.TournamentSceneIndex);
        }
    }
}
