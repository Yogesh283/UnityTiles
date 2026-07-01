using System;
using System.Collections;
using Mkey.Network;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Premium tournament matchmaking — live player cards, VS intro, WebSocket updates.
    /// </summary>
    public class TournamentWaitingRoomPanel : MonoBehaviour
    {
        private TournamentPremiumWaitingRoomView premiumView;
        private TournamentVsIntroView vsIntro;
        private TournamentDefinition tournament;
        private int currentPlayers;
        private int lastObservedPlayerCount;
        private float timeLeft;
        private Action onComplete;
        private bool launchStarted;
        private float searchPulse;
        private bool isVisible;
        private bool vsIntroPlayed;
        private bool isDuel;

        public bool IsShowing => isVisible && premiumView != null && premiumView.IsVisible;

        public void Show(TournamentDefinition data, Action completeCallback)
        {
            if (TournamentJoinDebug.IsFirstJoin(data))
                TournamentJoinDebug.Log("WaitingRoom.Show — premium multiplayer lobby");

            tournament = data;
            onComplete = completeCallback;
            launchStarted = false;
            searchPulse = 0f;
            vsIntroPlayed = false;
            isDuel = data != null && data.maxPlayers <= 2;
            lastObservedPlayerCount = 0;

            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(data.id);
            currentPlayers = snap.hasRoom ? snap.currentPlayers : 1;
            timeLeft = snap.hasRoom ? snap.countdownSeconds : data.waitingSeconds;

            StopAllCoroutines();
            TournamentApiBridge.RoomUpdated -= OnRoomUpdated;
            TournamentApiBridge.RoomUpdated += OnRoomUpdated;

            EnsureViews();
            premiumView.Show();
            RefreshView();
            StartCoroutine(WaitingRoutine());
        }

        public void Hide()
        {
            TournamentApiBridge.RoomUpdated -= OnRoomUpdated;
            StopAllCoroutines();
            vsIntro?.Hide();
            premiumView?.Hide();
            isVisible = false;
            launchStarted = false;
        }

        public static TournamentWaitingRoomPanel Create(Transform parent)
        {
            GameObject host = new GameObject("TournamentWaitingRoom");
            host.transform.SetParent(parent, false);
            TournamentWaitingRoomPanel view = host.AddComponent<TournamentWaitingRoomPanel>();
            view.EnsureViews();
            return view;
        }

        private void EnsureViews()
        {
            if (!premiumView)
                premiumView = TournamentPremiumWaitingRoomView.Create(transform);
            if (!vsIntro)
                vsIntro = TournamentVsIntroView.Create(transform);
        }

        private void OnRoomUpdated()
        {
            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
            if (snap.hasRoom)
            {
                currentPlayers = snap.currentPlayers;
                timeLeft = snap.countdownSeconds;
            }

            if (isDuel && lastObservedPlayerCount >= tournament.maxPlayers &&
                snap.currentPlayers < tournament.maxPlayers)
            {
                vsIntroPlayed = false;
                vsIntro?.Hide();
                if (launchStarted)
                {
                    launchStarted = false;
                    StopAllCoroutines();
                    StartCoroutine(WaitingRoutine());
                }
            }

            lastObservedPlayerCount = snap.currentPlayers;
            RefreshView();
        }

        private void RefreshView()
        {
            if (!premiumView || tournament == null)
                return;

            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
            premiumView.Bind(tournament, snap, searchPulse);
            isVisible = true;
        }

        private IEnumerator WaitingRoutine()
        {
            float fallbackPoll = 0f;
            float localSimStartedAt = Time.realtimeSinceStartup;

            while (!launchStarted)
            {
                searchPulse += Time.deltaTime;
                RefreshView();

                if (TournamentApiBridge.IsOnlineMode)
                {
                    fallbackPoll += Time.deltaTime;
                    if (fallbackPoll >= 1.0f)
                    {
                        fallbackPoll = 0f;
                        yield return RefreshApiRoomCoroutine();
                    }
                }

                TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
                if (snap.hasRoom)
                {
                    currentPlayers = snap.currentPlayers;
                    timeLeft = snap.countdownSeconds;
                }

                lastObservedPlayerCount = currentPlayers;

                if (TryHandleSearchTimeout(snap))
                {
                    yield return HandleSearchTimeoutRoutine();
                    yield break;
                }

                bool roomFull = currentPlayers >= tournament.maxPlayers;
                bool serverCountdown = snap.status == "starting";
                bool serverActive = snap.status == "active";

                if (isDuel && roomFull && (serverCountdown || serverActive))
                {
                    launchStarted = true;
                    yield return RunMatchStartSequence(snap);
                    yield break;
                }

                if (!isDuel && (serverCountdown || serverActive || snap.shouldLaunch) && roomFull)
                {
                    launchStarted = true;
                    yield return RunMatchStartSequence(snap);
                    yield break;
                }

                if (!TournamentApiBridge.IsOnlineMode && isDuel)
                {
                    if (Time.realtimeSinceStartup - localSimStartedAt >= tournament.waitingSeconds + 2f)
                    {
                        TournamentRoomRegistry.ForcePrepareForLaunch();
                        currentPlayers = tournament.maxPlayers;
                        snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
                        launchStarted = true;
                        RefreshView();
                        yield return RunMatchStartSequence(snap);
                        yield break;
                    }
                }

                if (!TournamentApiBridge.IsOnlineMode && !isDuel &&
                    Time.realtimeSinceStartup >= tournament.waitingSeconds + 5f)
                {
                    TournamentRoomRegistry.ForcePrepareForLaunch();
                    launchStarted = true;
                    RefreshView();
                    yield return new WaitForSeconds(0.8f);
                    onComplete?.Invoke();
                    Hide();
                    yield break;
                }

                yield return null;
            }
        }

        private bool TryHandleSearchTimeout(TournamentRoomSnapshot snap)
        {
            if (!TournamentApiBridge.IsOnlineMode || !isDuel || !snap.hasRoom)
                return false;
            if (currentPlayers >= tournament.maxPlayers)
                return false;
            if (snap.status != "waiting")
                return false;

            float remaining = snap.countdownSeconds;
            if (remaining > 0f)
                return false;

            return true;
        }

        private IEnumerator HandleSearchTimeoutRoutine()
        {
            launchStarted = true;
            Hide();

            if (NetworkManager.HasInstance)
            {
                var walletTask = WalletService.SyncToCoinsHolderAsync();
                while (!walletTask.IsCompleted)
                    yield return null;
            }

            TournamentApiBridge.Clear();
            TournamentJoinFlowGuard.Reset();
            TournamentSession.Clear();

            bool closed = false;
            TournamentMessagePopup.Show(
                "No Opponent Found",
                "We couldn't find an opponent in time.\n\nPlease try again.",
                () =>
                {
                    if (closed) return;
                    closed = true;
                    TournamentPageLifecycle.OnReturningFromMatch(null);
                },
                autoCloseSeconds: 4f);

            yield return new WaitForSecondsRealtime(4.5f);
        }

        private IEnumerator RunMatchStartSequence(TournamentRoomSnapshot snap)
        {
            if ((TournamentApiBridge.CurrentRoom?.serverNowMs ?? 0) > 0)
                TournamentServerClock.SyncServerTime(TournamentApiBridge.CurrentRoom.serverNowMs.Value);

            if (snap.matchStartAtMs > 0)
                TournamentServerClock.ScheduleServerStart(snap.matchStartAtMs);
            else if ((TournamentApiBridge.CurrentRoom?.matchStartAtMs ?? 0) > 0)
                TournamentServerClock.ScheduleServerStart(TournamentApiBridge.CurrentRoom.matchStartAtMs.Value);

            if (isDuel && currentPlayers >= tournament.maxPlayers)
            {
                RoomPlayerDto local = FindLocalPlayer(snap);
                RoomPlayerDto opponent = FindOpponentPlayer(snap);

                if (!vsIntroPlayed)
                {
                    vsIntroPlayed = true;
                    yield return vsIntro.PlayVsRevealRoutine(local, opponent);
                }

                yield return vsIntro.PlayServerCountdownRoutine();
            }
            else
            {
                while (TournamentServerClock.HasScheduledStart &&
                       TournamentServerClock.SecondsUntilStart() > 0.05f)
                {
                    searchPulse += Time.unscaledDeltaTime;
                    RefreshView();
                    yield return null;
                }
            }

            while (!TournamentServerClock.IsStartTimeReached())
            {
                searchPulse += Time.unscaledDeltaTime;
                RefreshView();
                yield return null;
            }

            RefreshView();
            yield return new WaitForSecondsRealtime(0.2f);
            onComplete?.Invoke();
            Hide();
        }

        private static RoomPlayerDto FindLocalPlayer(TournamentRoomSnapshot snap)
        {
            if (snap.players == null) return null;
            int localUserId = NetworkManager.HasInstance ? NetworkManager.Instance.UserId : 0;
            foreach (RoomPlayerDto player in snap.players)
            {
                if (player != null && player.userId == localUserId)
                    return player;
            }
            return snap.players.Count > 0 ? snap.players[0] : null;
        }

        private static RoomPlayerDto FindOpponentPlayer(TournamentRoomSnapshot snap)
        {
            if (snap.players == null) return null;
            int localUserId = NetworkManager.HasInstance ? NetworkManager.Instance.UserId : 0;
            foreach (RoomPlayerDto player in snap.players)
            {
                if (player != null && player.userId != localUserId)
                    return player;
            }
            return snap.players.Count > 1 ? snap.players[1] : null;
        }

        private IEnumerator RefreshApiRoomCoroutine()
        {
            var task = TournamentApiBridge.RefreshActiveRoomAsync();
            while (!task.IsCompleted)
                yield return null;
        }
    }
}
