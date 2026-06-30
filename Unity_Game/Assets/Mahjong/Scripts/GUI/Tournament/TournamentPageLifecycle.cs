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
            TournamentGlobalWaitingRoom.Hide();
            TournamentJoinFlowGuard.Reset();

            if (!TournamentMatchManager.HasActiveRoom)
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
