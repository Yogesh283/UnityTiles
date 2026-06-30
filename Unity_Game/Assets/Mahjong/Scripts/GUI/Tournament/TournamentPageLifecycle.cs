using System;
using Mkey.Network;

namespace Mkey.Tournament
{
    /// <summary>
    /// Tournament page show/hide cleanup — wallet sync, stale room/session reset.
    /// </summary>
    public static class TournamentPageLifecycle
    {
        public static void OnPageShown(Action refreshWallet)
        {
            // Keep active matchmaking overlay visible (OnEnable also fires on app resume).
            if (!TournamentGlobalWaitingRoom.IsVisible && !TournamentSession.IsActive)
                TournamentGlobalWaitingRoom.Hide();

            if (!TournamentGlobalWaitingRoom.IsVisible)
                TournamentJoinFlowGuard.Reset();

            if (!TournamentMatchManager.HasActiveRoom && !TournamentSession.IsActive)
                TournamentApiBridge.Clear();

            refreshWallet?.Invoke();
        }

        public static void OnReturningFromMatch(Action refreshWallet)
        {
            TournamentGlobalWaitingRoom.Hide();
            TournamentJoinFlowGuard.Reset();
            TournamentApiBridge.Clear();
            refreshWallet?.Invoke();
        }
    }
}
