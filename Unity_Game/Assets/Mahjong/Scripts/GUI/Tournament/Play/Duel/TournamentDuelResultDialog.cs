using System;

namespace Mkey.Tournament
{
    /// <summary>
    /// Legacy wrapper — use <see cref="TournamentMatchManager"/> instead.
    /// </summary>
    public static class TournamentDuelResultDialog
    {
        public static void ShowWin(int prizeCoins, Action onClosed) =>
            TournamentResultDialog.ShowDuelWin(prizeCoins, onClosed);

        public static void ShowLoss(Action onClosed) =>
            TournamentResultDialog.ShowDuelLoss(onClosed);

        public static void ReturnToTournamentPage() =>
            TournamentResultDialog.ReturnToTournamentPage();
    }
}
