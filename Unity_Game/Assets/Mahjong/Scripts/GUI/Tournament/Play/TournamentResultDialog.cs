using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mkey.Tournament
{
    public static class TournamentResultDialog
    {
        public static bool IsVisible => TournamentMessagePopup.IsVisible;

        private static string TournamentSubtitle()
        {
            TournamentDefinition tournament = TournamentSession.Tournament;
            if (tournament == null) return string.Empty;
            return $"{tournament.icon} {tournament.displayName}";
        }

        public static void ShowDuelWin(int prizeCoins, Action onClosed)
        {
            string subtitle = TournamentSubtitle();
            string header = string.IsNullOrEmpty(subtitle) ? string.Empty : subtitle + "\n\n";
            TournamentMessagePopup.Show(
                "YOU WIN!",
                header +
                $"Congratulations!\n\n+{prizeCoins:N0} Coins\nRank: #1",
                onClosed);
        }

        public static void ShowDuelLoss(Action onClosed)
        {
            string subtitle = TournamentSubtitle();
            string header = string.IsNullOrEmpty(subtitle) ? string.Empty : subtitle + "\n\n";
            TournamentMessagePopup.Show(
                "YOU LOSE",
                header +
                "Your opponent completed the level first.\n\nBetter luck next time.\n\nReward: 0 Coins",
                onClosed);
        }

        public static void ShowRankWin(int rank, int prizeCoins, Action onClosed)
        {
            string subtitle = TournamentSubtitle();
            string header = string.IsNullOrEmpty(subtitle) ? string.Empty : subtitle + "\n\n";
            TournamentMessagePopup.Show(
                "YOU WIN!",
                header +
                $"Congratulations!\n\n+{prizeCoins:N0} Coins\nRank: #{rank:N0}",
                onClosed);
        }

        public static void ShowRankLoss(string tournamentId, int rank, Action onClosed)
        {
            string subtitle = TournamentSubtitle();
            string header = string.IsNullOrEmpty(subtitle) ? string.Empty : subtitle + "\n\n";
            TournamentMessagePopup.Show(
                "YOU LOSE",
                header +
                $"Better luck next time.\n\nRank: #{rank:N0}\n\nReward: 0 Coins",
                onClosed);
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
