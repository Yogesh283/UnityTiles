using System.Collections;
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

            if (TournamentApiBridge.IsOnlineMode)
            {
                float waitTimeout = 12f;
                while (!TournamentServerClock.IsStartTimeReached() && waitTimeout > 0f)
                {
                    waitTimeout -= Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            TournamentMatchManager.BeginSynchronizedMatch();

            GameEvents.MatchSpritesEvent += OnMatchMade;
            timerHud = TournamentTimerHud.Create();
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
