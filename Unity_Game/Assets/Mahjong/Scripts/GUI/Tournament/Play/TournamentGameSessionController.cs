using System;
using System.Collections;
using System.Threading.Tasks;
using Mkey.Network;
using Mkey;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mkey.Tournament
{
    /// <summary>
    /// Tracks tournament timer and moves during the Mahjong game scene.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class TournamentGameSessionController : MonoBehaviour
    {
        private static TournamentGameSessionController instance;
        private static TournamentTimerHud timerHud;

        private float resultDialogWatchdog;
        private const float ResultDialogWatchdogSeconds = 2f;
        private const float OnlineDuelSyncTimeoutSeconds = 15f;
        private const float OnlineDuelForceStartGraceSeconds = 2f;
        // Anti-freeze cap for the lobby-launched duel start gate. Both clients hold the frozen board
        // until the shared server gameplay-start timestamp; if the server timing never arrives we
        // force-start after this many seconds so a client can never get stuck on a frozen board.
        private const float MaxLobbyStartSyncSeconds = 8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance) return;
            GameObject host = new GameObject(nameof(TournamentGameSessionController));
            instance = host.AddComponent<TournamentGameSessionController>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.buildIndex != TournamentSession.GameSceneIndex || !TournamentSession.IsActive)
            {
                StopTracking();
                return;
            }

            // GameLevelHolder.Awake resets CurrentLevel to 0 — re-apply before GameBoard.Start.
            TournamentSession.PrepareGameLevel();
            UiEventSystemGuard.EnforceSingle();
            StartCoroutine(BeginRound());
        }

        private void Update()
        {
            if (!TournamentSession.IsActive) return;
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != TournamentSession.GameSceneIndex)
                return;
            if (TournamentResultDialog.IsVisible || TournamentMatchManager.IsMatchResolved)
            {
                if (TournamentMatchManager.IsMatchResolved && !TournamentResultDialog.IsVisible)
                {
                    resultDialogWatchdog += Time.unscaledDeltaTime;
                    if (resultDialogWatchdog >= ResultDialogWatchdogSeconds)
                    {
                        resultDialogWatchdog = 0f;
                        TournamentMatchManager.TryApplyOnlineSnapshot();
                        TournamentMatchManager.ShowPendingResultDialog();
                    }
                }
                return;
            }

            if (TournamentMatchManager.IsMatchLocked)
            {
                TournamentMatchManager.EnsureGameplayFrozen();
                resultDialogWatchdog += Time.unscaledDeltaTime;
                if (resultDialogWatchdog >= ResultDialogWatchdogSeconds)
                {
                    resultDialogWatchdog = 0f;
                    TournamentMatchManager.TryApplyOnlineSnapshot();
                    TournamentMatchManager.ShowPendingResultDialog();
                }
                return;
            }

            if (TournamentMatchManager.IsWaitingForOpponentSync)
            {
                TournamentMatchManager.EnsureGameplayFrozen();
                return;
            }

            resultDialogWatchdog = 0f;

            if (Input.GetKeyDown(KeyCode.Escape))
                TournamentMatchManager.ForfeitAsLoss();
        }

        private IEnumerator BeginRound()
        {
            TournamentGameStartProbe.LogSceneEnter();
            yield return null;

            float timeout = 5f;
            while (GameBoard.Instance == null && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!TournamentSession.IsActive) yield break;

            yield return EnsureApiRoomSession();

            if (!TournamentMatchManager.HasActiveRoom && TournamentRoomRegistry.HasLocalRoom)
                TournamentMatchManager.AttachRoom(TournamentRoomRegistry.LocalRoom);

            if (!TournamentMatchManager.HasActiveRoom)
            {
                TournamentGameStartProbe.LogAbort("no active room — match timer not started");
                Debug.LogWarning("TournamentGameSessionController: no active room — match timer not started.");
                yield break;
            }

            if (!TournamentMatchManager.PrepareMatchFromRoom())
                TournamentRoomRegistry.ForcePrepareForLaunch();

            ReapplyServerClockFromApi();

            TournamentMatchManager.EnsureGameplayFrozen();
            TournamentFlowLog.BoardFrozen("begin round");

            if (TournamentApiBridge.IsOnlineMode && TournamentSession.Tournament != null &&
                TournamentSession.Tournament.maxPlayers <= 2)
            {
                if (TournamentSession.LobbyCountdownCompleted)
                    yield return WaitForLobbyLaunchedDuelStart();
                else
                    yield return WaitForOnlineDuelGameplayStart();
            }
            else if (TournamentApiBridge.IsOnlineMode)
            {
                yield return WaitForOnlineRaceGameplayStart();
            }

            if (!TournamentSession.IsActive)
            {
                TournamentGameStartProbe.LogAbort("session cleared during sync wait");
                yield break;
            }

            yield return UnlockGameplayWhenReady();
        }

        private static IEnumerator EnsureApiRoomSession()
        {
            if (TournamentApiBridge.HasMatchedRoom)
                yield break;

            string roomId = TournamentSession.ActiveRoomId;
            if (string.IsNullOrEmpty(roomId) || TournamentSession.Tournament == null)
                yield break;

            Task<ApiResult<RoomResponseDto>> fetch = TournamentService.FetchRoomSnapshotAsync(roomId);
            while (!fetch.IsCompleted)
                yield return null;

            if (fetch.Result.Success && fetch.Result.Data != null)
                TournamentApiBridge.ApplyRoomDto(TournamentSession.Tournament, fetch.Result.Data);
        }

        private static void ReapplyServerClockFromApi()
        {
            RoomResponseDto room = TournamentApiBridge.CurrentRoom;
            if (room == null)
                return;

            if ((room.serverNowMs ?? 0) > 0)
                TournamentServerClock.SyncServerTime(room.serverNowMs.Value);

            if ((room.matchStartAtMs ?? 0) > 0)
                TournamentServerClock.ScheduleServerStart(room.matchStartAtMs.Value);
        }

        private static IEnumerator WaitForLobbyLaunchedDuelStart()
        {
            // Server-authoritative synchronized start: BOTH duel clients keep the board frozen until
            // the SAME server timestamp (match_start_at_ms + buffer), then unfreeze together. This is
            // what makes two Android devices start the match at the same moment instead of "whenever
            // my scene finished loading". Bounded by MaxLobbyStartSyncSeconds so it can never hang.
            ReapplyServerClockFromApi();
            Debug.Log(
                "[TournamentGameStart] Lobby handoff — freezing board until shared server start " +
                $"(match_start_at_ms={TournamentServerClock.ScheduledStartMs} " +
                $"gameplay_start_ms={TournamentServerClock.GameplayStartMs})");
            TournamentFlowLog.BoardFrozen("lobby handoff — waiting for shared server gameplay-start time");

            float maxWait = MaxLobbyStartSyncSeconds;
            float refreshTimer = 0f;

            while (TournamentSession.IsActive && maxWait > 0f)
            {
                ReapplyServerClockFromApi();

                if (TournamentServerClock.IsGameplayStartTimeReached())
                {
                    Debug.Log(
                        "[TournamentGameStart] Shared server start time reached " +
                        $"(server_now>={TournamentServerClock.GameplayStartMs}) — unfreezing both clients");
                    TournamentFlowLog.BoardUnfrozen("shared server gameplay-start time reached");
                    yield break;
                }

                TournamentMatchManager.EnsureGameplayFrozen();

                // Periodically refresh so a client that missed the WebSocket push still learns
                // match_start_at_ms and stays aligned to the shared start timestamp.
                refreshTimer += Time.unscaledDeltaTime;
                if (refreshTimer >= 0.5f)
                {
                    refreshTimer = 0f;
                    Task<bool> refresh = TournamentApiBridge.RefreshActiveRoomAsync();
                    while (!refresh.IsCompleted)
                        yield return null;
                }

                maxWait -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!TournamentServerClock.IsGameplayStartTimeReached())
            {
                Debug.LogWarning(
                    "[TournamentGameStart] Shared start-time gate timed out after " +
                    $"{MaxLobbyStartSyncSeconds}s — force starting (anti-freeze fallback)");
                TournamentFlowLog.BoardUnfrozen("start-time gate timeout — force starting (anti-freeze)");
            }
        }

        private static IEnumerator WaitForOnlineDuelGameplayStart()
        {
            bool roomUpdated = false;
            Action onRoomUpdated = () => roomUpdated = true;
            TournamentApiBridge.RoomUpdated += onRoomUpdated;

            float timeout = OnlineDuelSyncTimeoutSeconds;
            float pollTimer = 0f;
            float sceneEnter = Time.realtimeSinceStartup;
            bool forceStart = false;

            TournamentFlowLog.BoardFrozen("waiting for opponent + server active sync");

            try
            {
                while (TournamentSession.IsActive && timeout > 0f)
                {
                    ReapplyServerClockFromApi();
                    RoomResponseDto apiRoom = TournamentApiBridge.CurrentRoom;

                    if (IsOnlineDuelGameplayReady(apiRoom))
                    {
                        TournamentGameStartProbe.LogSyncPoll(apiRoom);
                        break;
                    }

                    if (ShouldForceOnlineDuelStart(apiRoom, sceneEnter))
                    {
                        forceStart = true;
                        TournamentGameStartProbe.LogSyncPoll(apiRoom, forceStart: true);
                        break;
                    }

                    timeout -= Time.unscaledDeltaTime;
                    pollTimer += Time.unscaledDeltaTime;

                    if (roomUpdated || pollTimer >= 0.5f)
                    {
                        roomUpdated = false;
                        pollTimer = 0f;

                        if (apiRoom != null)
                        {
                            if (apiRoom.status == "starting")
                                TournamentFlowLog.Countdown(
                                    $"remaining={apiRoom.startCountdownSeconds}s players={apiRoom.playerCount}");
                            else if (apiRoom.playerCount >= 2 && apiRoom.status == "active")
                                TournamentFlowLog.MatchStart($"players={apiRoom.playerCount}");
                        }

                        Task<bool> refresh = TournamentApiBridge.RefreshActiveRoomAsync();
                        while (!refresh.IsCompleted)
                            yield return null;
                    }

                    TournamentMatchManager.EnsureGameplayFrozen();
                    yield return null;
                }
            }
            finally
            {
                TournamentApiBridge.RoomUpdated -= onRoomUpdated;
            }

            if (!TournamentSession.IsActive)
                yield break;

            if (!forceStart && !IsOnlineDuelGameplayReady(TournamentApiBridge.CurrentRoom))
            {
                TournamentFlowLog.BoardUnfrozen("forcing duel start after sync timeout");
                TournamentGameStartProbe.LogAbort("server sync timeout — force starting locally");
            }
        }

        private static IEnumerator WaitForOnlineRaceGameplayStart()
        {
            while (TournamentSession.IsActive && !TournamentServerClock.IsStartTimeReached())
            {
                ReapplyServerClockFromApi();
                TournamentMatchManager.EnsureGameplayFrozen();
                yield return null;
            }
        }

        private static bool IsOnlineDuelGameplayReady(RoomResponseDto apiRoom)
        {
            if (apiRoom == null || apiRoom.playerCount < 2)
                return false;

            if (TournamentSession.LobbyCountdownCompleted)
                return true;

            if (!TournamentServerClock.HasScheduledStart)
            {
                if ((apiRoom.matchStartAtMs ?? 0) > 0)
                    TournamentServerClock.ScheduleServerStart(apiRoom.matchStartAtMs.Value);
            }

            if (!TournamentServerClock.HasScheduledStart)
                return false;

            if (!TournamentServerClock.IsServerStartTimeReached())
                return false;

            string status = apiRoom.status ?? string.Empty;
            return status is "active" or "locked" or "starting";
        }

        private static bool ShouldForceOnlineDuelStart(RoomResponseDto apiRoom, float sceneEnterRealtime)
        {
            if (apiRoom == null || apiRoom.playerCount < 2)
                return false;

            if (TournamentSession.LobbyCountdownCompleted)
                return true;

            if (Time.realtimeSinceStartup - sceneEnterRealtime < OnlineDuelForceStartGraceSeconds)
                return false;

            if (TournamentServerClock.HasScheduledStart && TournamentServerClock.IsServerStartTimeReached())
                return true;

            string status = apiRoom.status ?? string.Empty;
            return status is "active" or "locked" or "starting";
        }

        private static IEnumerator UnlockGameplayWhenReady()
        {
            float levelTimeout = 8f;
            while (TournamentSession.IsActive &&
                   TournamentMatchManager.MatchLevelIndex < 0 &&
                   levelTimeout > 0f)
            {
                if (!TournamentMatchManager.PrepareMatchFromRoom())
                    TournamentRoomRegistry.ForcePrepareForLaunch();
                levelTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            TournamentGameStartProbe.LogBeforeBeginRound();
            Debug.Log("[TournamentGameStart] Countdown Finish (lobby) — unlocking board");

            if (!TournamentMatchManager.PrepareMatchFromRoom())
                TournamentRoomRegistry.ForcePrepareForLaunch();

            TournamentMatchManager.BeginSynchronizedMatch();

            if (!TournamentSession.GameplayRunning && TournamentSession.LobbyCountdownCompleted)
            {
                TournamentFlowLog.BoardUnfrozen("lobby handoff fallback — arming gameplay");
                TournamentMatchManager.BeginSynchronizedMatch();
            }

            bool gameplayRunning = TournamentSession.GameplayRunning;
            bool waitingForSync = TournamentMatchManager.IsWaitingForOpponentSync;

            if (GameBoard.Instance)
                GameBoard.Instance.SetControlActivity(true, true);

            bool boardUnlocked = GameBoard.Instance != null;
            TournamentGameStartProbe.LogAfterBeginRound(!waitingForSync, gameplayRunning, boardUnlocked);

            if (!gameplayRunning && TournamentSession.LobbyCountdownCompleted)
            {
                TournamentFlowLog.BoardUnfrozen("final fallback — force synchronized duel start");
                if (!TournamentMatchManager.PrepareMatchFromRoom())
                    TournamentRoomRegistry.ForcePrepareForLaunch();
                TournamentMatchManager.BeginSynchronizedMatch();
                gameplayRunning = TournamentSession.GameplayRunning;
            }

            if (!gameplayRunning && TournamentSession.LobbyCountdownCompleted)
            {
                TournamentSession.StartGameplayTracking();
                gameplayRunning = true;
            }

            if (GameBoard.Instance)
                GameBoard.Instance.SetControlActivity(true, true);

            if (gameplayRunning)
            {
                TournamentFlowLog.BoardUnfrozen("server active + match_start_at_ms reached");
                Debug.Log("[TournamentGameStart] Game Started");
                Debug.Log("[TournamentGameStart] Timer Started");
                Debug.Log("[TournamentGameStart] Board Unlocked");
                Debug.Log("[TournamentGameStart] Input Enabled");
                Debug.Log("[TournamentGameStart] Tiles Enabled");
            }
            else
            {
                TournamentFlowLog.BoardFrozen("BeginSynchronizedMatch did not arm gameplay");
                TournamentGameStartProbe.LogAbort("BeginSynchronizedMatch returned without starting");
                yield break;
            }

            // Idempotent subscribe: remove first so replays / fallback re-entry can never
            // double-count moves (duplicate listener => corrupted move/score sync).
            GameEvents.MatchSpritesEvent -= OnMatchMade;
            GameEvents.MatchSpritesEvent += OnMatchMade;

            if (!timerHud)
                timerHud = TournamentTimerHud.Create();
            TournamentGameStartProbe.LogGameStarted(timerHud != null);
        }

        private static void OnMatchMade(Sprite _, Sprite __)
        {
            TournamentSession.RegisterMove();
        }

        public static void StopTracking()
        {
            GameEvents.MatchSpritesEvent -= OnMatchMade;
            if (timerHud)
            {
                timerHud.Hide();
                timerHud = null;
            }
        }
    }
}
