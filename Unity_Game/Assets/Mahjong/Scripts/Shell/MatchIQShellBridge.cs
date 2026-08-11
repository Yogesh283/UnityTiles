using System;
using System.Collections;
using System.Collections.Generic;
using Mkey;
using Mkey.Tournament;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mkey.Shell
{
    /// <summary>
    /// Bridges Match IQ React Native UI ↔ Unity gameplay.
    /// Launch:  matchiqunity://play?matchId&amp;mode&amp;token[&amp;tournamentId][&amp;levelId]
    /// Return:  matchiq://match-result?matchId&amp;won&amp;score&amp;...
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class MatchIQShellBridge : MonoBehaviour
    {
        private const string ResultScheme = "matchiq://match-result";
        private const float LaunchDelaySeconds = 0.35f;

        // React Native drives real matches with a launch payload that arrives within a few hundred
        // ms. If none arrives within this window (Editor play-test, or a standalone Unity build with
        // no shell), we start a practice match so the player is never stuck on an unplayable board.
        private const float StandaloneFallbackSeconds = 2.5f;

        private static MatchIQShellBridge instance;
        private static bool? embeddedInShell;

        public static bool IsActive { get; private set; }

        /// <summary>True while RN PauseGame is active (menu open / app pause).</summary>
        public static bool IsPaused
        {
            get { return instance != null && instance.shellPaused; }
        }

        /// <summary>
        /// True only when Unity is running embedded inside the React Native shell (single APK). The
        /// shell's Unity view manager class is on the classpath only in that build, so a standalone /
        /// "local" Unity build and the Editor both report false. Used to decide whether to wait for a
        /// launch payload (embedded) or drop straight into gameplay with no pre-game page (local).
        /// </summary>
        private static bool IsEmbeddedInShell()
        {
            if (embeddedInShell.HasValue) return embeddedInShell.Value;

            bool embedded = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var jc = new AndroidJavaClass("com.azesmwayreactnativeunity.ReactNativeUnityViewManager"))
                {
                    embedded = jc.GetRawClass() != System.IntPtr.Zero;
                }
            }
            catch
            {
                embedded = false;
            }
#endif
            embeddedInShell = embedded;
            return embedded;
        }

        private string matchId;
        private string tournamentId;
        private string roomId;
        private string mode = "practice";
        private string token;
        private string levelId;
        private int pendingLevel = TournamentSession.SharedGameLevelIndex;
        private int pendingSeed;
        private float playStartedAt = -1f;
        private bool returning;
        private bool launchQueued;
        private bool launched;
        private int moveCount;
        private GameObject bootCurtain;
        private bool practiceFallbackArmed;
        private bool shellPaused;
        private int lastEmittedScore = int.MinValue;
        private int lastEmittedTimerSec = int.MinValue;
        private int lastEmittedMatches = int.MinValue;
        private bool matchesSubscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance) return;

            GameObject host = new GameObject(nameof(MatchIQShellBridge));
            instance = host.AddComponent<MatchIQShellBridge>();
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

            Application.deepLinkActivated += OnDeepLinkActivated;
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Gameplay is the only scene in the build, so Unity boots straight onto a board built
            // with default settings. Cover it until React Native tells us which match to run.
            ShowBootCurtain();
            TryConsumeUrl(Application.absoluteURL);

            // If Unity booted straight onto the gameplay scene (RN embed, or a standalone build with
            // no splash), arm the practice fallback so the board is playable even without a launch.
            // On the splash scene we do nothing here — the Start button drives the transition.
            if (SceneManager.GetActiveScene().buildIndex == TournamentSession.GameSceneIndex)
            {
                StartCoroutine(StripInitialSceneMenus());
                ArmPracticeFallback();
            }
            else if (!IsEmbeddedInShell())
            {
                // Local / standalone Unity booted onto a legacy menu scene (splash, map, tournament).
                // React Native owns all of those screens now, so skip the whole splash → Start →
                // play-tournament / play-level flow and drop straight onto the gameplay board.
                SceneManager.LoadScene(TournamentSession.GameSceneIndex);
            }
        }

        /// <summary>
        /// <see cref="SceneManager.sceneLoaded"/> never fires for the scene that is already open when
        /// the game boots (editor Play, or a device that starts straight on gameplay), so the level
        /// constructor / map / edit-mode chrome from the "Game Constructor" scene would linger until
        /// the first reload. Strip it across a few frames here — catching objects that only spawn in
        /// their own Awake/Start — so the very first frame shows nothing but the tile board + HUD.
        /// React Native owns every other screen, so the Unity view is gameplay-only by design.
        /// </summary>
        private IEnumerator StripInitialSceneMenus()
        {
            StripUnityMenus();
            yield return null;
            StripUnityMenus();
            yield return new WaitForSecondsRealtime(0.2f);
            StripUnityMenus();
        }

        /// <summary>
        /// Safety net so the game is playable "from start" even when no React Native launch signal
        /// arrives (Editor Play, standalone Unity APK, or a shell that failed to send the payload).
        /// A real RN launch flips <see cref="IsActive"/> before this fires, so it never interferes
        /// with the embedded flow. Only ever armed while the gameplay scene is active.
        /// </summary>
        private void ArmPracticeFallback()
        {
            if (practiceFallbackArmed || IsActive) return;
            practiceFallbackArmed = true;
            StartCoroutine(PracticeFallbackWhenIdle());
        }

        private IEnumerator PracticeFallbackWhenIdle()
        {
            // Only wait for a React Native launch when actually embedded in the shell. A standalone /
            // "local" Unity run (or the Editor) has no shell, so start gameplay immediately instead of
            // sitting on an empty pre-game page.
            float wait = IsEmbeddedInShell() ? StandaloneFallbackSeconds : 0f;
            yield return new WaitForSecondsRealtime(wait);
            practiceFallbackArmed = false;

            if (IsActive || launched || launchQueued) yield break;

            Debug.Log("[MatchIQShell] No React Native launch received — starting standalone practice match.");
            TryConsumeUrl(
                "matchiqunity://play?mode=practice&matchId=local-practice&level=" +
                TournamentSession.SharedGameLevelIndex);
        }

        /// <summary>
        /// Full-screen cover shown between Unity start-up and the launch payload from React Native,
        /// so the player never sees a stray default board.
        /// </summary>
        private void ShowBootCurtain()
        {
            if (bootCurtain || launched) return;

            // Only the embedded React Native shell sends a launch signal that lifts this curtain.
            // In the Editor or a standalone / "local" Unity build there is no shell, so the curtain
            // would never lift and would just be a black page in front of gameplay — skip it and let
            // the board show immediately.
            if (!IsEmbeddedInShell()) return;

            // Kill the default Unity grey clear-color flash before any scene content draws.
            ApplyObsidianCameraClear();

            bootCurtain = new GameObject("MatchIQBootCurtain");
            DontDestroyOnLoad(bootCurtain);

            Canvas canvas = bootCurtain.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var background = new GameObject("Background", typeof(UnityEngine.UI.Image));
            background.transform.SetParent(bootCurtain.transform, false);
            var image = background.GetComponent<UnityEngine.UI.Image>();
            // MatchIQ obsidian black — never flat Unity grey.
            image.color = new Color(0.02f, 0.02f, 0.04f, 1f);
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyObsidianCameraClear()
        {
            Color obsidian = new Color(0.02f, 0.02f, 0.04f, 1f);
            Camera[] cams = Camera.allCameras;
            for (int i = 0; i < cams.Length; i++)
            {
                Camera c = cams[i];
                if (!c) continue;
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = obsidian;
            }
            if (Camera.main)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = obsidian;
            }
        }

        private void HideBootCurtain()
        {
            if (!bootCurtain) return;
            Destroy(bootCurtain);
            bootCurtain = null;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                Application.deepLinkActivated -= OnDeepLinkActivated;
                SceneManager.sceneLoaded -= OnSceneLoaded;
                if (ScoreHolder.Instance)
                    ScoreHolder.Instance.ChangeEvent.RemoveListener(OnScoreChanged);
                if (matchesSubscribed && GameBoard.Instance)
                    GameBoard.Instance.ChangePossibleMatchesAction -= OnMatchesChanged;
                matchesSubscribed = false;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Never leave Unity menus / waiting rooms visible while embedded in the APK.
            StripUnityMenus();

            if (scene.buildIndex != TournamentSession.GameSceneIndex) return;

            if (IsEmbeddedInShell() || IsActive)
                ApplyObsidianCameraClear();

            // Gameplay scene loaded with nothing driving it (e.g. splash Start went straight here,
            // or a standalone build). Arm the practice fallback so it becomes playable.
            if (!IsActive)
            {
                ArmPracticeFallback();
                return;
            }

            // GameLevelHolder.Awake resets CurrentLevel to 0 — re-apply before GameBoard.Start.
            GameLevelHolder.CurrentLevel = pendingLevel;
            GameBoard.GMode = GameMode.Play;
            LevelBoosterResetService.ResetBoostersForNewMatch();

            // StripUnityMenus() (above) cleared the tournament session, so re-apply the room seed here,
            // before GameBoard.Start builds the grid, so both players get the identical board.
            if (pendingSeed != 0) TournamentSession.SetTileSeed(pendingSeed);
            if (playStartedAt < 0f)
            {
                CampaignLevelTimer.Reset();
                CampaignLevelTimer.Start();
                playStartedAt = Time.realtimeSinceStartup;
            }

            launched = true;
            lastEmittedTimerSec = int.MinValue;
            lastEmittedMatches = int.MinValue;
            StartCoroutine(FinalizeGameplayScene());
            EmitEvent(
                "GameStarted",
                $"\"matchId\":\"{EscapeJson(matchId)}\",\"level\":{pendingLevel + 1}");
            // Push the current timer immediately so RN can paint TIME without waiting a full second.
            ForceEmitTimer();
            TouchForensic.ResetSession();
            TouchForensic.Trace(0, "SESSION_RESET", "GameStarted — forensic counters cleared");
        }

        private IEnumerator FinalizeGameplayScene()
        {
            // Let GameBoard.Start rebuild the board, then force the obsidian background and hide
            // any leftover construct / tournament chrome that survived Awake.
            yield return null;
            StripUnityMenus();
            ForceGameplayBackground();
            UnblockBoardTaps();
            HideBootCurtain();
            SubscribeScoreEvents();
            SubscribeMatchesEvents();
            ForceEmitTimer();
        }

        private void SubscribeScoreEvents()
        {
            if (!ScoreHolder.Instance) return;
            ScoreHolder.Instance.ChangeEvent.RemoveListener(OnScoreChanged);
            ScoreHolder.Instance.ChangeEvent.AddListener(OnScoreChanged);
            OnScoreChanged(ScoreHolder.Count);
        }

        private void SubscribeMatchesEvents()
        {
            if (!GameBoard.Instance) return;
            GameBoard.Instance.ChangePossibleMatchesAction -= OnMatchesChanged;
            GameBoard.Instance.ChangePossibleMatchesAction += OnMatchesChanged;
            matchesSubscribed = true;
            OnMatchesChanged(GameBoard.Instance.GetPossibleMatchesCount());
        }

        private void OnScoreChanged(int score)
        {
            if (!IsActive) return;
            if (score == lastEmittedScore) return;
            lastEmittedScore = score;
            EmitEvent("ScoreUpdated", $"\"score\":{score}");
        }

        private void OnMatchesChanged(int matches)
        {
            if (!IsActive) return;
            if (matches == lastEmittedMatches) return;
            lastEmittedMatches = matches;
            EmitEvent("MatchesUpdated", $"\"matches\":{matches}");
        }

        private void ForceEmitTimer()
        {
            if (!IsActive) return;
            lastEmittedTimerSec = int.MinValue;
            EmitTimerIfNeeded();
        }

        /// <summary>
        /// Tiles are hit-tested by the world-space <c>TouchPad</c> (a full-screen bottom-most UI
        /// catcher). In shell mode React Native owns all chrome, so every other UI Graphic must
        /// drop <c>raycastTarget</c> — otherwise leftover HUD / themer / footer images sit above
        /// the TouchPad and swallow board taps while boosters still work via RN commands.
        /// </summary>
        private static void UnblockBoardTaps()
        {
            TouchPad pad = UnityEngine.Object.FindFirstObjectByType<TouchPad>();
            // Always ensure TouchPad is active so board taps register regardless of any
            // prior SetControlActivity(false) call that may have locked it during init.
            if (pad) pad.SetTouchActivity(true);
            Graphic padGraphic = pad ? pad.GetComponent<Graphic>() : null;

            foreach (Graphic g in UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!g) continue;
                if (g == padGraphic || g.GetComponentInParent<TouchPad>())
                {
                    // TouchPad must stay raycastable so EventSystem delivers board taps.
                    g.raycastTarget = true;
                    if (!g.enabled) g.enabled = true;
                    continue;
                }

                if (!g.raycastTarget) continue;

                // Shell owns chrome — nuke all competing UI raycasts (not only full-screen / muted).
                if (IsActive)
                {
                    g.raycastTarget = false;
                    continue;
                }

                CanvasGroup cg = g.GetComponentInParent<CanvasGroup>();
                bool mutedGroup = cg && (cg.alpha < 0.05f || !cg.blocksRaycasts || !cg.interactable);

                Selectable sel = g.GetComponentInParent<Selectable>();
                bool deadSelectable = sel && !sel.IsInteractable();

                if (mutedGroup || deadSelectable || CoversViewport(g.rectTransform))
                    g.raycastTarget = false;
            }
        }

        private static bool CoversViewport(RectTransform rt)
        {
            if (!rt) return false;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float w = Mathf.Abs(corners[2].x - corners[0].x);
            float h = Mathf.Abs(corners[2].y - corners[0].y);
            // Overlay-canvas corners are in screen pixels; treat >= 85% of the screen as full-screen.
            return w >= Screen.width * 0.85f && h >= Screen.height * 0.85f;
        }

        /// <summary>
        /// APK shell owns lobby / pay / results — destroy Unity-only UI so splash, map, construct
        /// and tournament pay/waiting screens never appear on top of gameplay.
        /// </summary>
        private static void StripUnityMenus()
        {
            if (TournamentSession.IsActive)
                TournamentSession.Clear();

            TournamentGlobalWaitingRoom.Hide();

            GameBoard.GMode = GameMode.Play;

            foreach (GameConstructor construct in UnityEngine.Object.FindObjectsByType<GameConstructor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (construct) Destroy(construct.gameObject);
            }

            string[] hideNames =
            {
                "CanvasConstruct",
                "gConstructor",
                "ButtonToMap",
                "Play_Edit_ModeButton",
                "TournamentPage",
                "TournamentPremiumWaitingRoom",
                "WaitingRoom",
            };
            foreach (string name in hideNames)
            {
                GameObject go = GameObject.Find(name);
                if (go) Destroy(go);
            }
        }

        /// <summary>
        /// Prefer the premium black obsidian gameplay background when the shell owns the match;
        /// fall back to GameObjectsSet index 0 otherwise.
        /// </summary>
        private static void ForceGameplayBackground()
        {
            GameBoard board = UnityEngine.Object.FindFirstObjectByType<GameBoard>();
            if (!board) return;

            Sprite bg = Resources.Load<Sprite>("GameBackgrounds/BkgObsidianTemple");
            if (!bg && GameConstructSet.Instance && GameConstructSet.Instance.GOSet != null)
                bg = GameConstructSet.Instance.GOSet.GetBackGround(0);
            if (bg) board.BackGround = bg;
        }

        /// <summary>Counts matched pairs so React Native can post real move counts to the server.</summary>
        public static void RegisterMove()
        {
            if (instance) instance.moveCount++;
        }

        private float nextInputEnsureAt;
        private float lastRawTapProbeAt;
        private bool touchPadGotPointerThisTouch;

        private void Update()
        {
            if (!IsActive || returning) return;

            // Android back / Esc → ask React Native for exit confirmation (do not forfeit immediately).
            if (Input.GetKeyDown(KeyCode.Escape))
                EmitEvent("ExitRequested", $"\"matchId\":\"{EscapeJson(matchId)}\"");

            EmitTimerIfNeeded();
            // BoardDirectTouchController owns Input.GetTouch tile selection — stop EventSystem miss probes.
            if (!BoardDirectTouchController.IsAuthoritative)
                ProbeRawTouchVsTouchPad();

            if (Time.unscaledTime >= nextInputEnsureAt)
            {
                nextInputEnsureAt = Time.unscaledTime + 0.35f;
                EnsureShellPlayable();
            }
        }

        /// <summary>
        /// If Android delivers a finger to Unity Input but TouchPad never got EventSystem
        /// OnPointerDown, RN is (or was) stealing the touch — or a UI Graphic ate the raycast.
        /// </summary>
        private void ProbeRawTouchVsTouchPad()
        {
            TouchPad pad = TouchPad.Instance;
            if (pad && pad.IsTouched) touchPadGotPointerThisTouch = true;

            if (Input.touchCount <= 0)
            {
                touchPadGotPointerThisTouch = false;
                return;
            }

            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Canceled)
            {
                TouchForensic.Trace(TouchPad.CurrentPressId, "RAW_CANCELED",
                    $"fingerId={t.fingerId} — expect press state clear on Exit/Up");
            }

            if (t.phase != TouchPhase.Began) return;
            if (Time.unscaledTime - lastRawTapProbeAt < 0.05f) return;
            lastRawTapProbeAt = Time.unscaledTime;

            int pressId = TouchForensic.BeginPress();
            TouchForensic.RawReceived++;
            TouchPad.PendingRawPressId = pressId;
            TouchPad.PendingRawScreen = t.position;
            TouchPad.PendingRawTime = Time.unscaledTime;
            TouchPad.PendingRawFingerId = t.fingerId;
            touchPadGotPointerThisTouch = false;

            TouchForensic.Trace(pressId, "RAW_TOUCH_RECEIVED",
                $"fingerId={t.fingerId} touchCount={Input.touchCount} phase={t.phase} " +
                $"RAW_TOUCH_POSITION=({t.position.x:F1},{t.position.y:F1}) " +
                $"UNITY_SCREEN_SIZE=({Screen.width}x{Screen.height})");

            StartCoroutine(ReportRawTapIfMissed(pressId, t.position));
        }

        private IEnumerator ReportRawTapIfMissed(int pressId, Vector2 screenPos)
        {
            yield return null;
            yield return null;
            if (touchPadGotPointerThisTouch) yield break;
            if (TouchPad.PendingRawPressId != pressId) yield break; // PointerDown already consumed it
            if (!IsActive || returning) yield break;

            TouchPad pad = TouchPad.Instance;
            bool padActive = pad && pad.IsActive;
            TouchPad.PendingRawPressId = 0;
            TouchForensic.Fail(pressId,
                padActive ? "PointerDown_NOT_RECEIVED" : "TOUCHPAD_INACTIVE",
                $"RAW=YES POINTER_DOWN=NO screen=({screenPos.x:F0},{screenPos.y:F0})");

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Vector3 wp = Camera.main ? Camera.main.ScreenToWorldPoint(screenPos) : Vector3.zero;
            string cam = Camera.main
                ? Camera.main.name + " z=" + Camera.main.transform.position.z.ToString("F1")
                : "NULL";
            EmitTapDiag("raw-miss", 0, screenPos.x, screenPos.y, wp.x, wp.y, padActive, cam);
#endif
        }

        private void EmitTimerIfNeeded()
        {
            if (!CampaignLevelTimer.IsRunning && !CampaignLevelTimer.IsPaused) return;
            int sec = Mathf.CeilToInt(CampaignLevelTimer.RemainingSeconds);
            if (sec == lastEmittedTimerSec) return;
            lastEmittedTimerSec = sec;
            EmitEvent(
                "TimerUpdated",
                $"\"remainingSeconds\":{sec},\"formatted\":\"{EscapeJson(CampaignLevelTimer.FormatRemaining())}\"");
        }

        private void OnDeepLinkActivated(string url) => TryConsumeUrl(url);

        private void TryConsumeUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            if (!url.StartsWith("matchiqunity://", StringComparison.OrdinalIgnoreCase)) return;

            Dictionary<string, string> q = ParseQuery(url);
            matchId = Get(q, "matchId", $"match-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}");
            tournamentId = Get(q, "tournamentId", null);
            roomId = Get(q, "roomId", null);
            mode = Get(q, "mode", "practice");
            token = Get(q, "token", null);
            levelId = Get(q, "levelId", null);

            pendingLevel = TournamentSession.SharedGameLevelIndex;
            if (!string.IsNullOrEmpty(levelId) && int.TryParse(levelId, out int parsed))
                pendingLevel = Mathf.Max(0, parsed);

            // Server-assigned per-room seed so both players build the exact same board. 0 = no seed
            // (practice / dev) which falls back to the shared default arrangement.
            pendingSeed = 0;
            string seedStr = Get(q, "seed", null);
            if (!string.IsNullOrEmpty(seedStr) && int.TryParse(seedStr, out int parsedSeed))
                pendingSeed = parsedSeed;

            IsActive = true;
            returning = false;
            launched = false;
            playStartedAt = -1f;
            moveCount = 0;

            Debug.Log($"[MatchIQShell] Launch received matchId={matchId} mode={mode} level={pendingLevel}");

            if (!launchQueued)
            {
                launchQueued = true;
                StartCoroutine(LaunchGameplayWhenReady());
            }
        }

        private IEnumerator LaunchGameplayWhenReady()
        {
            yield return new WaitForSecondsRealtime(LaunchDelaySeconds);

            GameLevelHolder.CurrentLevel = pendingLevel;
            LevelBoosterResetService.ResetBoostersForNewMatch();
            CampaignLevelTimer.Reset();
            CampaignLevelTimer.Start();
            playStartedAt = Time.realtimeSinceStartup;

            // Clear any leftover tournament session so campaign win path runs
            if (TournamentSession.IsActive)
                TournamentSession.Clear();

            int gameScene = TournamentSession.GameSceneIndex;
            Debug.Log($"[MatchIQShell] Loading gameplay scene {gameScene} level={pendingLevel}");

            // Gameplay is already the boot scene, so this is a reload that rebuilds the board with
            // the level the server picked for this room. SceneLoader's progress popup would just be
            // a second loading screen on top of React Native's, so load directly.
            SceneManager.LoadScene(gameScene);

            launchQueued = false;
        }

        /// <summary>Hands control back to React Native without a win (home, back, time up).</summary>
        public static void ReturnToShell(bool won)
        {
            if (instance) instance.moveCount = Mathf.Max(instance.moveCount, 0);
            ReturnMatchResult(won);
        }

        /// <summary>Campaign clock expired while the shell owns the match — report the loss.</summary>
        public static bool TryHandleTimeUp()
        {
            if (!IsActive) return false;
            CampaignLevelTimer.Stop();
            ReturnMatchResult(false);
            return true;
        }

        /// <summary>
        /// Called when the shell-driven match finishes (win/lose).
        /// Opens the RN app via deep link with result payload.
        /// </summary>
        public static void ReturnMatchResult(bool won)
        {
            if (!instance || !IsActive || instance.returning) return;
            instance.returning = true;

            int score = ScoreHolder.Instance ? ScoreHolder.Count : 0;
            float elapsed = instance.playStartedAt > 0f
                ? Mathf.Max(1f, Time.realtimeSinceStartup - instance.playStartedAt)
                : Mathf.Max(1f, CampaignLevelTimer.ElapsedSeconds);
            int moves = Mathf.Max(1, instance.moveCount);

            // Existing Unity result metrics (VictoryWindow accuracy ratio + ScoreController combo).
            // Do not invent a new IQ formula — expose what the game already computes.
            int avg = ScoreHolder.AverageScore > 0 ? ScoreHolder.AverageScore : Mathf.Max(1, score);
            float iq = score / (float)avg * 100f;
            int combo = 0;
            ScoreController sc = UnityEngine.Object.FindFirstObjectByType<ScoreController>(FindObjectsInactive.Include);
            if (sc) combo = sc.MaxCombo;
            string message = BuildResultMessage(won, combo, iq);

            // Rewards and ranking are decided by the server from score/moves/time — Unity only
            // reports what actually happened on the board.
            var p = new Dictionary<string, string>
            {
                ["matchId"] = instance.matchId ?? $"match-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                ["won"] = won ? "true" : "false",
                ["score"] = score.ToString(),
                ["timeSeconds"] = Mathf.RoundToInt(elapsed).ToString(),
                ["moves"] = moves.ToString(),
                ["iq"] = iq.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture),
                ["combo"] = combo.ToString(),
                ["accuracy"] = iq.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture),
                ["message"] = message,
                ["level"] = (instance.pendingLevel + 1).ToString(),
            };
            if (!string.IsNullOrEmpty(instance.tournamentId))
                p["tournamentId"] = instance.tournamentId;
            if (!string.IsNullOrEmpty(instance.roomId))
                p["roomId"] = instance.roomId;

            string url = ResultScheme + "?" + ToQuery(p);
            // Also send JSON for embedded UnityView (same APK)
            string iqJson = iq.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            string json =
                "{" +
                $"\"type\":\"match-result\"," +
                $"\"matchId\":\"{EscapeJson(p["matchId"])}\"," +
                $"\"won\":{(won ? "true" : "false")}," +
                $"\"score\":{score}," +
                $"\"timeSeconds\":{Mathf.RoundToInt(elapsed)}," +
                $"\"moves\":{moves}," +
                $"\"iq\":{iqJson}," +
                $"\"combo\":{combo}," +
                $"\"accuracy\":{iqJson}," +
                $"\"message\":\"{EscapeJson(message)}\"," +
                $"\"level\":{instance.pendingLevel + 1}" +
                (string.IsNullOrEmpty(instance.tournamentId)
                    ? ""
                    : $",\"tournamentId\":\"{EscapeJson(instance.tournamentId)}\"") +
                (string.IsNullOrEmpty(instance.roomId)
                    ? ""
                    : $",\"roomId\":\"{EscapeJson(instance.roomId)}\"") +
                "}";

            Debug.Log($"[MatchIQShell] Returning to RN: {url}");

            // Tell RN the outcome as a typed event as well as the legacy match-result payload.
            EmitEvent(
                won ? "LevelCompleted" : "GameOver",
                $"\"matchId\":\"{EscapeJson(p["matchId"])}\",\"won\":{(won ? "true" : "false")},\"score\":{score},\"timeSeconds\":{Mathf.RoundToInt(elapsed)},\"moves\":{moves},\"iq\":{iqJson},\"combo\":{combo},\"accuracy\":{iqJson},\"message\":\"{EscapeJson(message)}\",\"level\":{instance.pendingLevel + 1}");

            IsActive = false;
            instance.launched = false;
            CampaignLevelTimer.Stop();
            instance.ShowBootCurtain();

            bool delivered = false;
            try
            {
                delivered = SendToReactNative(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MatchIQShell] Embedded RN message failed: " + ex.Message);
            }

            // Deep link is the fallback for the legacy standalone build. Firing it while embedded
            // would bounce the user out to a second app instance.
            if (delivered) return;

            try
            {
                Application.OpenURL(url);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>Called from React Native UnityView.postMessage.</summary>
        public void OnReactNativeLaunch(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            Debug.Log("[MatchIQShell] OnReactNativeLaunch " + json);
            try
            {
                // Lightweight parse without full JSON lib
                string asUrl = "matchiqunity://play?" + JsonObjectToQuery(json);
                TryConsumeUrl(asUrl);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// RN → Unity commands (also callable as named methods below).
        /// JSON: {"command":"UseHint"} or {"command":"PauseGame"} …
        /// </summary>
        public void OnReactNativeCommand(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            string cmd = ExtractJsonString(json, "command");
            if (string.IsNullOrEmpty(cmd)) cmd = ExtractJsonString(json, "type");
            if (string.IsNullOrEmpty(cmd)) return;
            int amount = 1;
            string amountStr = ExtractJsonString(json, "amount");
            if (!string.IsNullOrEmpty(amountStr) && int.TryParse(amountStr, out int parsed) && parsed > 0)
                amount = parsed;
            DispatchCommand(cmd, amount);
        }

        // Named entry points for react-native-unity postMessage(go, method, message).
        public void PauseGame(string _) => DispatchCommand("PauseGame");
        public void ResumeGame(string _) => DispatchCommand("ResumeGame");
        public void RestartGame(string _) => DispatchCommand("RestartGame");
        public void UseHint(string _) => DispatchCommand("UseHint");
        public void ShuffleTiles(string _) => DispatchCommand("ShuffleTiles");
        public void UndoMove(string _) => DispatchCommand("UndoMove");
        public void ExitGame(string _) => DispatchCommand("ExitGame");
        public void GrantShuffle(string msg) => DispatchCommand("GrantShuffle", ParseAmount(msg, 2));
        public void GrantUndo(string msg) => DispatchCommand("GrantUndo", ParseAmount(msg, 2));
        public void WatchRewardedAd(string _) => DispatchCommand("WatchRewardedAd");

        private void DispatchCommand(string command, int amount = 1)
        {
            if (string.IsNullOrEmpty(command)) return;
            Debug.Log("[MatchIQShell] Command " + command);

            switch (command)
            {
                case "PauseGame":
                    shellPaused = true;
                    CampaignLevelTimer.Pause();
                    Time.timeScale = 0f;
                    EmitEvent("GamePaused", "\"paused\":true");
                    break;

                case "ResumeGame":
                    shellPaused = false;
                    CampaignLevelTimer.Resume();
                    Time.timeScale = 1f;
                    EnsureBoardInputEnabled();
                    EmitEvent("GamePaused", "\"paused\":false");
                    break;

                case "RestartGame":
                    shellPaused = false;
                    Time.timeScale = 1f;
                    TouchManager.ClearSelectionState();
                    CampaignLevelTimer.Reset();
                    CampaignLevelTimer.Start();
                    playStartedAt = Time.realtimeSinceStartup;
                    moveCount = 0;
                    lastEmittedScore = int.MinValue;
                    lastEmittedTimerSec = int.MinValue;
                    lastEmittedMatches = int.MinValue;
                    LevelBoosterResetService.ResetBoostersForNewMatch();
                    if (GameBoard.Instance) GameBoard.Instance.RestartLevel();
                    EnsureBoardInputEnabled();
                    EmitEvent(
                        "GameStarted",
                        $"\"matchId\":\"{EscapeJson(matchId)}\",\"level\":{pendingLevel + 1},\"restarted\":true");
                    ForceEmitTimer();
                    TouchForensic.ResetSession();
                    TouchForensic.Trace(0, "SESSION_RESET", "GameStarted — counters cleared");
                    break;

                case "UseHint":
                    ApplyHintFromShell();
                    break;

                case "ShuffleTiles":
                    ApplyShuffleFromShell();
                    break;

                case "UndoMove":
                    ApplyUndoFromShell();
                    break;

                case "GrantShuffle":
                    if (ShuffleHolder.Instance) ShuffleHolder.Add(Mathf.Max(1, amount));
                    EmitEvent("RewardGranted", $"\"kind\":\"shuffle\",\"amount\":{Mathf.Max(1, amount)},\"count\":{(ShuffleHolder.Instance ? ShuffleHolder.Count : 0)}");
                    break;

                case "GrantUndo":
                    if (UndoHolder.Instance) UndoHolder.Add(Mathf.Max(1, amount));
                    EmitEvent("RewardGranted", $"\"kind\":\"undo\",\"amount\":{Mathf.Max(1, amount)},\"count\":{(UndoHolder.Instance ? UndoHolder.Count : 0)}");
                    break;

                case "WatchRewardedAd":
                    ApplyWatchRewardedAdFromShell();
                    break;

                case "ExitGame":
                    Time.timeScale = 1f;
                    shellPaused = false;
                    // Already reported a result — only force a loss if still mid-match.
                    if (IsActive && !returning) ReturnMatchResult(false);
                    break;

                default:
                    Debug.LogWarning("[MatchIQShell] Unknown command: " + command);
                    break;
            }
        }

        /// <summary>
        /// Existing AdsControl rewarded path — on success grants boosters via holders (no new economy).
        /// </summary>
        private static void ApplyWatchRewardedAdFromShell()
        {
            void Grant()
            {
                if (ShuffleHolder.Instance) ShuffleHolder.Add(1);
                if (UndoHolder.Instance) UndoHolder.Add(1);
                EmitEvent(
                    "RewardGranted",
                    $"\"kind\":\"video\",\"amount\":1,\"shuffle\":{(ShuffleHolder.Instance ? ShuffleHolder.Count : 0)},\"undo\":{(UndoHolder.Instance ? UndoHolder.Count : 0)}");
            }

            if (AdsControl.Instance)
            {
                AdsControl.Instance.ShowRewardedAd(
                    "rewardedad",
                    null,
                    null,
                    (ok, _type, _amt) =>
                    {
                        if (ok) Grant();
                        else EmitEvent("RewardGranted", "\"kind\":\"video\",\"amount\":0,\"ok\":false");
                    });
                return;
            }

            // Editor / builds without ads — still honour the button via existing holders.
            Grant();
        }

        private static string BuildResultMessage(bool won, int combo, float iq)
        {
            if (!won) return "Tough round — rematch and climb back up!";
            if (combo >= 12)
                return "Not one fumble, you identified\nlocked tiles accurately!";
            if (combo >= 6)
                return "Sharp streak — your combo\nkept the board under control!";
            if (iq >= 100f)
                return "Brilliant clear — you outpaced\nthe average score target!";
            return "Well played — keep climbing\nand protect your rank!";
        }

        private static int ParseAmount(string msg, int fallback)
        {
            if (!string.IsNullOrEmpty(msg) && int.TryParse(msg, out int n) && n > 0) return n;
            return fallback;
        }

        private static void ApplyHintFromShell()
        {
            GameBoard board = GameBoard.Instance;
            if (!board) return;
            if (board.IsAlreadyHint()) return;
            if (HintHolder.Instance && HintHolder.Count <= 0) return;

            // Call board directly — do not open Unity "Get Free Hint" popups (RN owns UI).
            board.TrySelectHintMatch(good =>
            {
                if (good && HintHolder.Instance && HintHolder.Count > 0)
                    HintHolder.Add(-1);
            });
            EnsureBoardInputEnabled();
        }

        private static void ApplyShuffleFromShell()
        {
            GameBoard board = GameBoard.Instance;
            if (!board) return;
            if (ShuffleHolder.Instance && ShuffleHolder.Count <= 0) return;

            // ShuffleGrid briefly disables TouchPad during tweens. If timeScale is 0 or a tween
            // stalls, taps stay dead — always re-enable input on a realtime schedule afterward.
            board.ShuffleGrid(null);
            if (ShuffleHolder.Instance) ShuffleHolder.Add(-1);
            if (instance) instance.StartCoroutine(instance.ReenableInputSoon());
        }

        private static void ApplyUndoFromShell()
        {
            if (UndoHolder.Instance && UndoHolder.Count <= 0) return;
            UndoMatch undo = UnityEngine.Object.FindFirstObjectByType<UndoMatch>(FindObjectsInactive.Include);
            if (undo) undo.RestoreUndoState();
            EnsureBoardInputEnabled();
        }

        private IEnumerator ReenableInputSoon()
        {
            for (int i = 0; i < 16; i++)
            {
                yield return new WaitForSecondsRealtime(0.2f);
                if (shellPaused) continue;
                EnsureBoardInputEnabled();
            }
        }

        /// <summary>
        /// Keep board tappable while the shell is active and not paused: lift boot curtains,
        /// clear full-screen UI raycast blockers, and force TouchPad back on.
        /// </summary>
        private void EnsureShellPlayable()
        {
            if (!IsActive || returning) return;

            // Destroy any leftover black boot curtain that still swallows touches.
            GameObject curtain = GameObject.Find("MatchIQBootCurtain");
            if (curtain) Destroy(curtain);
            if (bootCurtain && launched)
            {
                Destroy(bootCurtain);
                bootCurtain = null;
            }

            UnblockBoardTaps();

            if (!shellPaused)
            {
                if (GameBoard.GMode != GameMode.Play)
                    GameBoard.GMode = GameMode.Play;
                if (Mathf.Abs(Time.timeScale - 1f) > 0.01f)
                    Time.timeScale = 1f;
                EnsureBoardInputEnabled();
                // Collect/shuffle may leave IsActive false if a finally/reenable path stalled.
                if (TouchPad.Instance)
                    TouchPad.Instance.TryRecoverStuckInactive(2.5f);
                // Authoritative board taps: Input.GetTouch → BoardDirectTouchController.
                BoardDirectTouchController.EnsureExists();
                DisablePlayModeCellColliders();
            }
        }

        /// <summary>
        /// Construct-mode cell colliders must stay off in Play — otherwise OverlapPoint can hit a
        /// GridCell (no tile select) instead of the MahjongTile on top.
        /// </summary>
        private static void DisablePlayModeCellColliders()
        {
            if (GameBoard.GMode != GameMode.Play) return;
            GameBoard board = GameBoard.Instance;
            if (!board || board.MainGrid == null || board.MainGrid.Cells == null) return;
            for (int i = 0; i < board.MainGrid.Cells.Count; i++)
            {
                GridCell cell = board.MainGrid.Cells[i];
                if (!cell) continue;
                Collider2D col = cell.GetComponent<Collider2D>();
                if (col && col.enabled) col.enabled = false;
            }
        }

        private static void EnsureBoardInputEnabled()
        {
            try
            {
                // TouchPad must keep a raycastable Graphic so EventSystem delivers board taps.
                TouchPad pad = TouchPad.Instance
                    ? TouchPad.Instance
                    : UnityEngine.Object.FindFirstObjectByType<TouchPad>();
                if (pad)
                {
                    if (!pad.IsActive) pad.SetTouchActivity(true);
                    Graphic g = pad.GetComponent<Graphic>();
                    if (g)
                    {
                        g.enabled = true;
                        g.raycastTarget = true;
                        Color c = g.color;
                        if (c.a < 0.01f) { c.a = 0.01f; g.color = c; }
                    }
                }

                if (GameBoard.Instance)
                    GameBoard.Instance.SetControlActivity(true, true);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MatchIQShell] EnsureBoardInput: " + ex.Message);
                if (TouchPad.Instance) TouchPad.Instance.SetTouchActivity(true);
            }
        }

        private static void EmitEvent(string type, string fieldsCsv)
        {
            string json = string.IsNullOrEmpty(fieldsCsv)
                ? $"{{\"type\":\"{type}\"}}"
                : $"{{\"type\":\"{type}\",{fieldsCsv}}}";
            try { SendToReactNative(json); }
            catch (Exception ex) { Debug.LogWarning("[MatchIQShell] Emit failed: " + ex.Message); }
        }

        /// <summary>
        /// Board-tap probe for development builds only — never emit TapDiag to RN in release APKs.
        /// </summary>
        public static void EmitTapDiag(
            string source,
            int hits,
            float sx,
            float sy,
            float wx,
            float wy,
            bool padActive,
            string cam,
            string hitName = "",
            string free = "-",
            int layer = -1)
        {
#if !(DEVELOPMENT_BUILD || UNITY_EDITOR)
            return;
#else
            if (!IsActive) return;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string camSafe = EscapeJson(cam ?? "");
            string srcSafe = EscapeJson(source ?? "");
            string hitSafe = EscapeJson(hitName ?? "");
            string freeSafe = EscapeJson(free ?? "-");
            EmitEvent(
                "TapDiag",
                $"\"source\":\"{srcSafe}\",\"hits\":{hits}," +
                $"\"sx\":{sx.ToString("0", inv)},\"sy\":{sy.ToString("0", inv)}," +
                $"\"wx\":{wx.ToString("0.##", inv)},\"wy\":{wy.ToString("0.##", inv)}," +
                $"\"pad\":{(padActive ? "true" : "false")},\"cam\":\"{camSafe}\"," +
                $"\"hit\":\"{hitSafe}\",\"free\":\"{freeSafe}\",\"layer\":{layer}");
#endif
        }

        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;
            string token = "\"" + key + "\"";
            int i = json.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;
            int colon = json.IndexOf(':', i + token.Length);
            if (colon < 0) return null;
            int start = colon + 1;
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
            if (start >= json.Length) return null;
            if (json[start] == '"')
            {
                int end = json.IndexOf('"', start + 1);
                if (end < 0) return null;
                return json.Substring(start + 1, end - start - 1);
            }
            int j = start;
            while (j < json.Length && json[j] != ',' && json[j] != '}') j++;
            return json.Substring(start, j - start).Trim();
        }

        private static bool SendToReactNative(string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var jc = new AndroidJavaClass("com.azesmwayreactnativeunity.ReactNativeUnityViewManager"))
                {
                    jc.CallStatic("sendMessageToMobileApp", message);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MatchIQShell] ReactNativeUnityViewManager missing: " + ex.Message);
            }
#endif
            Debug.Log("[MatchIQShell] RN message (editor/fallback): " + message);
            return false;
        }

        private static string EscapeJson(string s) =>
            (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string JsonObjectToQuery(string json)
        {
            // Expect flat JSON: {"matchId":"...","mode":"practice",...}
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string body = json.Trim();
            if (body.StartsWith("{")) body = body.Substring(1);
            if (body.EndsWith("}")) body = body.Substring(0, body.Length - 1);
            string[] parts = body.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                int colon = part.IndexOf(':');
                if (colon <= 0) continue;
                string key = part.Substring(0, colon).Trim().Trim('"');
                string val = part.Substring(colon + 1).Trim().Trim('"');
                map[key] = val;
            }
            return ToQuery(map);
        }

        public static bool TryHandleCampaignWin()
        {
            if (!IsActive) return false;
            CampaignLevelTimer.Stop();
            ReturnMatchResult(true);
            return true;
        }

        private static string Get(Dictionary<string, string> q, string key, string fallback)
        {
            if (q != null && q.TryGetValue(key, out string v) && !string.IsNullOrEmpty(v))
                return Uri.UnescapeDataString(v);
            return fallback;
        }

        private static Dictionary<string, string> ParseQuery(string url)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int qIndex = url.IndexOf('?');
            if (qIndex < 0 || qIndex >= url.Length - 1) return result;

            string query = url.Substring(qIndex + 1);
            string[] pairs = query.Split('&');
            for (int i = 0; i < pairs.Length; i++)
            {
                string pair = pairs[i];
                if (string.IsNullOrEmpty(pair)) continue;
                int eq = pair.IndexOf('=');
                if (eq <= 0)
                {
                    result[Uri.UnescapeDataString(pair)] = string.Empty;
                    continue;
                }

                string key = Uri.UnescapeDataString(pair.Substring(0, eq));
                string value = Uri.UnescapeDataString(pair.Substring(eq + 1));
                result[key] = value;
            }

            return result;
        }

        private static string ToQuery(Dictionary<string, string> p)
        {
            var parts = new List<string>(p.Count);
            foreach (KeyValuePair<string, string> kv in p)
            {
                if (kv.Value == null) continue;
                parts.Add(Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value));
            }

            return string.Join("&", parts);
        }
    }
}
