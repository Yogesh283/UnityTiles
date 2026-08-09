using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Replaces the tiny top-left menu/burger button with a clear "Leave" button during gameplay,
    /// so the player can always exit the match back to the React Native app. The old menu button is
    /// hidden and a labelled red button is dropped into the top-left corner, wired straight to
    /// <see cref="Shell.MatchIQShellBridge.ReturnToShell(bool)"/>. Runtime-only and revertible.
    /// </summary>
    [DefaultExecutionOrder(260)]
    public class GameLeaveButton : MonoBehaviour
    {
        // Legacy top-left buttons to hide (burger / menu / pause).
        private static readonly string[] OldButtonNames = { "ButtonMenu", "MenuButton", "PauseButton" };

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
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // top-left
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(26f, -26f);
            rt.sizeDelta = new Vector2(196f, 76f);
            rt.SetAsLastSibling();

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
            txt.fontSize = 30;
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
