using System;
using Mkey.Network;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Tournament page show/hide cleanup — wallet sync, stale room/session reset.
    /// </summary>
    public static class TournamentPageLifecycle
    {
        private const string Tag = "[TournamentJoinFlowGuard]";

        public static void OnPageShown(Action refreshWallet)
        {
            TournamentJoinFlowGuard.LogState("TournamentPageLifecycle.OnPageShown (before cleanup)");

            // Keep active matchmaking overlay visible (OnEnable also fires on app resume).
            if (!TournamentGlobalWaitingRoom.IsVisible && !TournamentSession.IsActive)
                TournamentGlobalWaitingRoom.Hide();

            if (!TournamentGlobalWaitingRoom.IsVisible)
            {
                TournamentJoinFlowGuard.ResetFrom();
            }
            else
            {
                Debug.LogWarning(
                    $"{Tag} Reset SKIPPED in OnPageShown — " +
                    "TournamentGlobalWaitingRoom.IsVisible == true (matchmaking overlay still active)");
            }

            if (!TournamentMatchManager.HasActiveRoom && !TournamentSession.IsActive)
                TournamentApiBridge.Clear();

            refreshWallet?.Invoke();
            TournamentJoinFlowGuard.LogState("TournamentPageLifecycle.OnPageShown (after cleanup)");
        }

        public static void OnReturningFromMatch(Action refreshWallet)
        {
            TournamentJoinFlowGuard.LogState("TournamentPageLifecycle.OnReturningFromMatch (before cleanup)");
            TournamentGlobalWaitingRoom.Hide();
            TournamentJoinFlowGuard.ResetFrom();
            TournamentApiBridge.Clear();
            refreshWallet?.Invoke();
            TournamentJoinFlowGuard.LogState("TournamentPageLifecycle.OnReturningFromMatch (after cleanup)");
        }
    }
}
