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
        private int lastLoggedPlayerCount;
        private float timeLeft;
        private Action onComplete;
        private bool launchStarted;
        private bool launchSequenceRunning;
        private float searchPulse;
        private bool isVisible;
        private bool vsIntroPlayed;
        private bool isDuel;
        private float waitRoomShownAt;
        private string lastLoggedPhase;

        private const float DuelPollIntervalSeconds = 1.0f;
        private const float MultiPollIntervalSeconds = 1.0f;

        private float roomPollTimer;
        private bool immediatePollDone;

        public bool IsShowing => isVisible && premiumView != null && premiumView.IsVisible;

        public void Show(TournamentDefinition data, Action completeCallback)
        {
            if (TournamentJoinDebug.IsFirstJoin(data))
                TournamentJoinDebug.Log("WaitingRoom.Show — premium multiplayer lobby");

            isVisible = true;
            tournament = data;
            onComplete = completeCallback;
            launchStarted = false;
            launchSequenceRunning = false;
            searchPulse = 0f;
            vsIntroPlayed = false;
            isDuel = data != null && data.maxPlayers <= 2;
            lastObservedPlayerCount = 0;
            lastLoggedPlayerCount = 0;
            waitRoomShownAt = Time.realtimeSinceStartup;
            lastLoggedPhase = string.Empty;
            roomPollTimer = 0f;
            immediatePollDone = false;
            ResetTransitionProbe();
            TournamentFlowLog.Searching("waiting room panel shown");

            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(data.id);
            currentPlayers = snap.hasRoom ? snap.currentPlayers : 1;
            timeLeft = snap.hasRoom ? snap.countdownSeconds : data.waitingSeconds;

            StopAllCoroutines();
            TournamentApiBridge.RoomUpdated -= OnRoomUpdated;
            TournamentApiBridge.RoomUpdated += OnRoomUpdated;

            EnsureViews();
            premiumView.Show(CancelMatchmaking);
            RefreshView();
            StartCoroutine(WaitingRoutine());
        }

        public void CancelMatchmaking()
        {
            if (launchStarted)
                return;

            AbortWaitingState("user cancelled from waiting room");
        }

        private void AbortWaitingState(string reason)
        {
            TournamentFlowLog.RoomClosed(reason);
            TournamentFlowLog.WaitingState("cancelled", reason);
            TournamentJoinCoordinator.NotifyWaitingRoomClosed();
            TournamentApiBridge.RoomUpdated -= OnRoomUpdated;
            StopAllCoroutines();
            TournamentRoomWebSocket.StopMaintainingConnection();
            TournamentApiBridge.Clear();
            TournamentSession.Clear();
            TournamentJoinFlowGuard.Reset();
            vsIntro?.Hide();
            premiumView?.Hide();
            isVisible = false;
            launchStarted = false;
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
            TryBeginLaunchFromRoomUpdate(snap);
        }

        private void TryBeginLaunchFromRoomUpdate(TournamentRoomSnapshot snap)
        {
            if (!TryClaimLaunch(snap))
                return;

            TournamentFlowLog.GameStart(
                $"trigger=room_updated status={snap.status} players={snap.currentPlayers}/{tournament.maxPlayers}");
            StartCoroutine(RunMatchStartSequence(snap));
        }

        private bool TryClaimLaunch(TournamentRoomSnapshot snap)
        {
            if (launchStarted || launchSequenceRunning || tournament == null)
                return false;

            if (!CanLaunchMatch(snap))
                return false;

            launchStarted = true;
            return true;
        }

        private bool CanLaunchMatch(TournamentRoomSnapshot snap)
        {
            int players = snap.hasRoom ? snap.currentPlayers : currentPlayers;
            bool roomFull = players >= tournament.maxPlayers;
            if (!roomFull)
                return false;

            bool serverReady = !TournamentApiBridge.IsOnlineMode || TournamentApiBridge.HasActiveApiSession;
            if (!serverReady)
                return false;

            EnsureServerClockFromApi();

            string status = snap.status ?? string.Empty;
            if (snap.shouldLaunch)
                return true;

            return status is "starting" or "active" or "locked";
        }

        private static void EnsureServerClockFromApi()
        {
            RoomResponseDto room = TournamentApiBridge.CurrentRoom;
            if (room == null)
                return;

            if ((room.serverNowMs ?? 0) > 0)
                TournamentServerClock.SyncServerTime(room.serverNowMs.Value);

            if ((room.matchStartAtMs ?? 0) > 0)
                TournamentServerClock.ScheduleServerStart(room.matchStartAtMs.Value);
        }

        private void RefreshView()
        {
            if (!premiumView || tournament == null)
                return;

            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
            float clientWaitSeconds = GetClientWaitSecondsRemaining(snap);
            premiumView.Bind(tournament, snap, searchPulse, clientWaitSeconds);
            isVisible = true;
            LogWaitingPhaseFromSnapshot(snap, snap.hasRoom ? snap.currentPlayers : 1);
        }

        private void LogWaitingPhaseFromSnapshot(TournamentRoomSnapshot snap, int players)
        {
            string phase;
            if (!TournamentApiBridge.HasActiveApiSession)
                phase = TournamentApiBridge.IsBackgroundJoinActive ? "searching" : "joining";
            else if (players < tournament.maxPlayers)
                phase = "searching";
            else if (snap.status == "starting")
                phase = "starting";
            else if (snap.status == "active" || snap.status == "locked")
                phase = "active";
            else
                phase = "found";

            if (players >= 2 && lastLoggedPlayerCount < 2)
                TournamentFlowLog.PlayerFound($"players={players}/{tournament.maxPlayers}");

            if (players >= tournament.maxPlayers && lastLoggedPlayerCount < tournament.maxPlayers)
                TournamentFlowLog.RoomFull($"players={players}/{tournament.maxPlayers}");

            lastLoggedPlayerCount = players;

            if (snap.status == "starting" && lastLoggedPhase != "starting")
                TournamentFlowLog.CountdownStart(
                    $"players={players} countdown={snap.countdownSeconds:F1}s match_start_at_ms={snap.matchStartAtMs}");

            LogWaitingPhase(phase, $"players={players}/{tournament.maxPlayers} status={snap.status}");
        }

        private void LogWaitingPhase(string phase, string detail)
        {
            if (phase == lastLoggedPhase)
                return;

            lastLoggedPhase = phase;
            TournamentFlowLog.WaitingState(phase, detail);
        }

        private void ResetTransitionProbe()
        {
            TournamentTransitionProbe.Reset();
        }

        private float GetElapsedSearchSeconds() =>
            Mathf.Max(0f, Time.realtimeSinceStartup - waitRoomShownAt);

        private float GetClientWaitSecondsRemaining(TournamentRoomSnapshot snap)
        {
            if (snap.hasRoom && snap.status == "starting" && snap.countdownSeconds > 0f)
                return snap.countdownSeconds;

            if (snap.hasRoom && snap.countdownSeconds > 0f &&
                (snap.status == "active" || snap.status == "locked"))
                return snap.countdownSeconds;

            if (!TournamentApiBridge.HasActiveApiSession ||
                (snap.hasRoom && snap.currentPlayers < tournament.maxPlayers))
                return GetElapsedSearchSeconds();

            return Mathf.Max(0f, tournament.waitingSeconds - GetElapsedSearchSeconds());
        }

        private IEnumerator WaitingRoutine()
        {
            float fallbackPoll = 0f;
            float localSimStartedAt = Time.realtimeSinceStartup;

            while (!launchStarted)
            {
                searchPulse += Time.deltaTime;
                RefreshView();

                if (TournamentApiBridge.IsOnlineMode && TournamentApiBridge.HasMatchedRoom)
                {
                    float pollInterval = isDuel ? DuelPollIntervalSeconds : MultiPollIntervalSeconds;
                    if (!immediatePollDone)
                    {
                        immediatePollDone = true;
                        yield return RefreshApiRoomCoroutine();
                    }

                    roomPollTimer += Time.deltaTime;
                    if (roomPollTimer >= pollInterval)
                    {
                        roomPollTimer = 0f;
                        yield return RefreshApiRoomCoroutine();
                    }
                }
                else if (TournamentApiBridge.IsOnlineMode)
                {
                    fallbackPoll += Time.deltaTime;
                    if (fallbackPoll >= 1f)
                    {
                        fallbackPoll = 0f;
                        RefreshView();
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

                EnsureServerClockFromApi();

                if (TryClaimLaunch(snap))
                {
                    TournamentFlowLog.GameStart(
                        $"trigger=waiting_routine status={snap.status} players={snap.currentPlayers}/{tournament.maxPlayers}");
                    yield return RunMatchStartSequence(snap);
                    yield break;
                }

                bool roomFull = currentPlayers >= tournament.maxPlayers;
                bool serverCountdown = snap.status == "starting";
                bool serverActive = snap.status == "active" || snap.status == "locked";
                bool serverReady = TournamentApiBridge.IsOnlineMode
                    ? TournamentApiBridge.HasActiveApiSession
                    : true;
                bool willEnter = isDuel && roomFull && serverReady && (serverCountdown || serverActive || snap.shouldLaunch);

                if (isDuel && roomFull && serverReady)
                    TournamentTransitionProbe.LogWaitingGate(
                        snap, tournament.maxPlayers, roomFull, serverCountdown, serverActive, willEnter);

                if (!isDuel && snap.shouldLaunch && roomFull && serverReady)
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
            if (!TournamentApiBridge.IsOnlineMode || !isDuel)
                return false;
            if (currentPlayers >= tournament.maxPlayers)
                return false;

            if (GetElapsedSearchSeconds() < tournament.waitingSeconds)
                return false;

            string status = snap.hasRoom ? (snap.status ?? "waiting") : "waiting";
            if (status != "waiting" && status != "starting")
                return false;

            return true;
        }

        private IEnumerator HandleSearchTimeoutRoutine()
        {
            launchStarted = true;
            int balanceBefore = CoinsHolder.Instance ? CoinsHolder.Count : 0;
            AbortWaitingState("no player found in time");

            if (NetworkManager.HasInstance)
            {
                var walletTask = WalletService.SyncToCoinsHolderAsync();
                while (!walletTask.IsCompleted)
                    yield return null;

                if (walletTask.Result.Success)
                {
                    int balanceAfter = walletTask.Result.Data;
                    if (balanceAfter > balanceBefore)
                        TournamentFlowLog.RefundCompleted($"balance={balanceAfter} refunded={balanceAfter - balanceBefore}");
                    else
                        TournamentFlowLog.RefundCompleted($"balance synced={balanceAfter}");
                }
                else
                {
                    TournamentFlowLog.ApiRetry($"wallet sync after timeout err={walletTask.Result.ErrorMessage}");
                }
            }

            bool closed = false;
            TournamentMessagePopup.Show(
                "No player found.",
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
            if (launchSequenceRunning)
                yield break;

            launchSequenceRunning = true;
            try
            {
            TournamentTransitionProbe.LogRunMatchStartEntered(snap);
            EnsureServerClockFromApi();

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

                if (TournamentServerClock.HasScheduledStart)
                    yield return vsIntro.PlayServerCountdownRoutine();
                else
                    TournamentFlowLog.CountdownStart("skipped — no match_start_at_ms yet; launching when server time reached");
            }
            else
            {
                TournamentTransitionProbe.LogCountdownStarted();
                TournamentFlowLog.CountdownStart("multiplayer lobby countdown");
                while (TournamentServerClock.HasScheduledStart &&
                       TournamentServerClock.SecondsUntilStart() > 0.05f)
                {
                    searchPulse += Time.unscaledDeltaTime;
                    RefreshView();
                    yield return null;
                }
            }

            float launchWait = 12f;
            while (!TournamentServerClock.IsStartTimeReached() && launchWait > 0f)
            {
                EnsureServerClockFromApi();
                searchPulse += Time.unscaledDeltaTime;
                RefreshView();
                launchWait -= Time.unscaledDeltaTime;
                yield return null;
            }

            RefreshView();
            yield return new WaitForSecondsRealtime(0.2f);
            TournamentTransitionProbe.LogLaunchGameFromWaitingRoom();
            TournamentFlowLog.GameStarted($"tournament={tournament.id} room={snap.roomId}");
            onComplete?.Invoke();
            Hide();
            }
            finally
            {
                launchSequenceRunning = false;
            }
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
