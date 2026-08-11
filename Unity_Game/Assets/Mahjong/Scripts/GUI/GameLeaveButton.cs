using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mkey.Shell;

namespace Mkey
{
    /// <summary>
    /// Adds a clear "Leave" button to the top-left of the gameplay header so the player can always
    /// exit the match back to the React Native app, wired to
    /// <see cref="Shell.MatchIQShellBridge.ReturnToShell(bool)"/>. The original burger menu
    /// (ButtonMenu) is left in place on the RIGHT of the header (positioned by CampaignTimerHud) so
    /// the in-game pause/options popup is still reachable. Runtime-only and revertible.
    /// </summary>
    [DefaultExecutionOrder(260)]
    public class GameLeaveButton : MonoBehaviour
    {
        // Stray legacy buttons to hide (duplicate menu / pause). ButtonMenu is intentionally NOT here
        // — it stays on the right of the header as the in-game menu option.
        private static readonly string[] OldButtonNames = { "MenuButton", "PauseButton" };

        // Vertical centre matches CampaignTimerHud.HeaderY — Leave stays level with the HUD row.
        private const float HeaderY = -148f;
        // Extra left padding so Leave clears LEVEL on 1080 / 1220 phones.
        private const float LeftMargin = 20f;
        // Compact Leave (~68px tall) matching HUD visual height, not the full section box.
        private static readonly Vector2 ButtonSize = new Vector2(128f, 68f);

        // Cached so we can keep the button parked in the banner after rotation / safe-area changes.
        private RectTransform leaveRt;
        private Canvas leaveCanvas;
        private int lastW = -1;
        private int lastH = -1;
        private float lastTop = float.NaN;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryInstall(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryInstall(scene);

        private static void TryInstall(Scene scene)
        {
            if (scene.buildIndex != Tournament.TournamentSession.GameSceneIndex) return;
            if (FindFirstObjectByType<GameLeaveButton>()) return;
            new GameObject(nameof(GameLeaveButton)).AddComponent<GameLeaveButton>();
        }

        private IEnumerator Start()
        {
            // Let the header UI build first.
            yield return null;
            yield return null;
            HideOldButtons();
            CreateLeaveButton();
            // The header can rebuild late; re-apply once so our button survives.
            yield return new WaitForSecondsRealtime(0.4f);
            HideOldButtons();
            CreateLeaveButton();
        }

        private static void HideOldButtons()
        {
            for (int i = 0; i < OldButtonNames.Length; i++)
            {
                GameObject go = GameObject.Find(OldButtonNames[i]);
                if (go) go.SetActive(false);
            }
        }

        private void CreateLeaveButton()
        {
            // React Native renders Leave + exit confirmation when the shell owns the match.
            if (MatchIQShellBridge.IsActive)
            {
                // Destroy any stale Leave from a previous non-shell session.
                GameObject stale = GameObject.Find("LeaveNowButton");
                if (stale) MatchIQShellHudHider.HideVisuals(stale);
                return;
            }

            Canvas canvas = ResolveCanvas();
            if (!canvas) return;
            if (canvas.transform.Find("LeaveNowButton")) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject btnGo = new GameObject(
                "LeaveNowButton",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(canvas.transform, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            // Anchor to the top-left but pivot on the LEFT-MIDDLE so anchoredPosition.y is the button's
            // vertical centre — we line it up with the header row (HeaderY), pushed below the safe area.
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = ButtonSize;
            rt.SetAsLastSibling();

            leaveRt = rt;
            leaveCanvas = canvas;
            PositionLeave();

            Image img = btnGo.GetComponent<Image>();
            img.color = new Color(0.89f, 0.11f, 0.14f, 0.96f); // WXO brand red
            Outline outline = btnGo.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.4f, 0.85f); // gold rim
            outline.effectDistance = new Vector2(1.6f, -1.6f);

            GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtGo.transform.SetParent(rt, false);
            Text txt = txtGo.GetComponent<Text>();
            txt.font = font;
            txt.text = "\u2039 Leave";
            txt.fontSize = 26;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
            RectTransform trt = txt.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            Button btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(OnLeave);
        }

        // Parks the button inside the header banner: left edge (past any landscape punch-hole) and
        // vertically centred on the header row, dropped below the status bar / notch safe area.
        private void PositionLeave()
        {
            if (!leaveRt) return;
            float top = WxoSafeArea.TopInsetCanvas(leaveCanvas);
            float left = LeftMargin + WxoSafeArea.LeftInsetCanvas(leaveCanvas);
            leaveRt.anchoredPosition = new Vector2(left, HeaderY - top);
            lastW = Screen.width;
            lastH = Screen.height;
            lastTop = top;
        }

        private void Update()
        {
            if (!leaveRt) return;
            float top = WxoSafeArea.TopInsetCanvas(leaveCanvas);
            if (Screen.width != lastW || Screen.height != lastH || !Mathf.Approximately(top, lastTop))
                PositionLeave();
        }

        private void OnLeave()
        {
            // Hand the match back to React Native (no win). In the embedded APK the shell owns the
            // session, so this returns to the app; harmless if pressed outside an active session.
            Shell.MatchIQShellBridge.ReturnToShell(false);
        }

        private Canvas ResolveCanvas()
        {
            if (HeaderGUIController.Instance)
            {
                Canvas c = HeaderGUIController.Instance.GetComponentInParent<Canvas>();
                if (c) return c.rootCanvas;
            }

            GameObject canvasMain = GameObject.Find("CanvasMain");
            if (canvasMain)
            {
                Canvas c = canvasMain.GetComponent<Canvas>();
                if (c) return c.rootCanvas;
            }

            Canvas any = FindFirstObjectByType<Canvas>();
            return any ? any.rootCanvas : null;
        }
    }
}
