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
            yield return null;

            float timeout = 5f;
            while (GameBoard.Instance == null && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!TournamentSession.IsActive) yield break;

            if (!TournamentMatchManager.HasActiveRoom && TournamentRoomRegistry.HasLocalRoom)
                TournamentMatchManager.AttachRoom(TournamentRoomRegistry.LocalRoom);

            if (!TournamentMatchManager.HasActiveRoom)
            {
                Debug.LogWarning("TournamentGameSessionController: no active room — match timer not started.");
                yield break;
            }

            if (!TournamentMatchManager.PrepareMatchFromRoom())
                TournamentRoomRegistry.ForcePrepareForLaunch();

            TournamentMatchManager.EnsureGameplayFrozen();
            TournamentFlowLog.BoardFrozen("begin round");

            if (TournamentApiBridge.IsOnlineMode && TournamentSession.Tournament != null &&
                TournamentSession.Tournament.maxPlayers <= 2)
            {
                yield return WaitForInstantDuelSyncStart();
            }
            else if (TournamentApiBridge.IsOnlineMode)
            {
                while (TournamentSession.IsActive && !TournamentServerClock.IsStartTimeReached())
                {
                    TournamentMatchManager.EnsureGameplayFrozen();
                    yield return null;
                }
            }

            RoomResponseDto readyRoom = TournamentApiBridge.CurrentRoom;
            if (!TournamentSession.IsActive ||
                (TournamentApiBridge.IsOnlineMode &&
                 TournamentSession.Tournament != null &&
                 TournamentSession.Tournament.maxPlayers <= 2 &&
                 (readyRoom == null || readyRoom.status != "active" ||
                  !TournamentServerClock.IsServerStartTimeReached())))
            {
                TournamentFlowLog.BoardFrozen("abort — server never confirmed active start");
                yield break;
            }

            TournamentMatchManager.BeginSynchronizedMatch();
            TournamentFlowLog.BoardUnfrozen("server active + match_start_at_ms reached");

            if (GameBoard.Instance)
                GameBoard.Instance.SetControlActivity(true, true);

            GameEvents.MatchSpritesEvent += OnMatchMade;
            timerHud = TournamentTimerHud.Create();
        }

        private static IEnumerator WaitForInstantDuelSyncStart()
        {
            float pollTimer = 0f;
            TournamentFlowLog.BoardFrozen("waiting for opponent + server active sync");

            while (TournamentSession.IsActive)
            {
                RoomResponseDto apiRoom = TournamentApiBridge.CurrentRoom;

                if (apiRoom != null &&
                    apiRoom.playerCount >= 2 &&
                    apiRoom.status == "active" &&
                    TournamentServerClock.HasScheduledStart &&
                    TournamentServerClock.IsServerStartTimeReached())
                    break;

                pollTimer += Time.unscaledDeltaTime;
                if (pollTimer >= 1.0f)
                {
                    pollTimer = 0f;
                    Task<bool> refresh = TournamentApiBridge.RefreshActiveRoomAsync();
                    while (!refresh.IsCompleted)
                        yield return null;

                    apiRoom = TournamentApiBridge.CurrentRoom;
                    if (apiRoom != null)
                    {
                        if (apiRoom.status == "starting")
                            TournamentFlowLog.Countdown(
                                $"remaining={apiRoom.startCountdownSeconds}s players={apiRoom.playerCount}");
                        else if (apiRoom.playerCount >= 2 && apiRoom.status == "active")
                            TournamentFlowLog.MatchStart($"players={apiRoom.playerCount}");
                    }
                }

                TournamentMatchManager.EnsureGameplayFrozen();
                yield return null;
            }
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
