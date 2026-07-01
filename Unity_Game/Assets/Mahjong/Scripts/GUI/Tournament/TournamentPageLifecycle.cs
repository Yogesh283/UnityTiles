using System;
using Mkey.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Tournament page show/hide cleanup — wallet sync, stale room/session reset.
    /// </summary>
    public static class TournamentPageLifecycle
    {
        public static void OnPageShown(Action refreshWallet)
        {
            TournamentJoinFlowGuard.LogState("TournamentPageLifecycle.OnPageShown (before cleanup)");

            bool clearedStaleOverlay = TournamentGlobalWaitingRoom.ClearStaleOnPageOpen();
            ClearStalePremiumOverlay();
            ClearHiddenFullscreenRaycastBlockers();

            if (clearedStaleOverlay || !TournamentJoinFlowGuard.IsActiveMatchmaking)
                TournamentJoinFlowGuard.ResetFrom();

            if (!TournamentMatchManager.HasActiveRoom && !TournamentSession.IsActive)
                TournamentApiBridge.Clear();

            refreshWallet?.Invoke();
            TournamentJoinFlowGuard.LogState("TournamentPageLifecycle.OnPageShown (after cleanup)");
        }

        private static void ClearStalePremiumOverlay()
        {
            if (!TournamentPremiumOverlay.IsVisible || TournamentJoinFlowGuard.IsActiveMatchmaking)
                return;

            TournamentPremiumOverlay.ForceDismiss();
        }

        /// <summary>Disable raycasts on hidden full-screen tournament overlays above the page canvas.</summary>
        private static void ClearHiddenFullscreenRaycastBlockers()
        {
            if (TournamentGlobalWaitingRoom.IsVisible)
                return;

            const int tournamentPageSortOrder = 100;
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (!canvas || canvas.sortingOrder <= tournamentPageSortOrder)
                    continue;

                if (canvas.gameObject.activeInHierarchy && canvas.enabled)
                    continue;

                Image[] images = canvas.GetComponentsInChildren<Image>(true);
                for (int j = 0; j < images.Length; j++)
                {
                    Image image = images[j];
                    if (!image || !image.raycastTarget)
                        continue;

                    RectTransform rt = image.rectTransform;
                    if (!rt)
                        continue;

                    bool fullScreen =
                        rt.anchorMin == Vector2.zero &&
                        rt.anchorMax == Vector2.one &&
                        rt.offsetMin == Vector2.zero &&
                        rt.offsetMax == Vector2.zero;

                    if (fullScreen)
                        image.raycastTarget = false;
                }
            }
        }

        public static void OnReturningFromMatch(Action refreshWallet)
        {
            TournamentJoinFlowGuard.LogState("TournamentPageLifecycle.OnReturningFromMatch (before cleanup)");
            TournamentGlobalWaitingRoom.DestroyStaleOverlay();
            TournamentPremiumOverlay.ForceDismiss();
            TournamentJoinFlowGuard.ResetFrom();
            TournamentApiBridge.Clear();
            refreshWallet?.Invoke();
            TournamentJoinFlowGuard.LogState("TournamentPageLifecycle.OnReturningFromMatch (after cleanup)");
        }
    }
}
