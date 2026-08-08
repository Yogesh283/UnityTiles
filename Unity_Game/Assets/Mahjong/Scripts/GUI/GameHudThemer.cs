using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Reskins the in-game power-up HUD to sit inside the emerald background's painted slots:
    /// hides the wooden footer tray and drops the three power-up buttons straight into the three
    /// gold circles painted on the background.
    ///
    /// Placement is bound to the actual background <see cref="SpriteRenderer"/>: we take each
    /// circle's normalized position inside the artwork, project it through the game camera to a
    /// screen point, and convert that to a canvas position. This lands the icons exactly on the
    /// painted circles regardless of camera framing or CanvasScaler settings (a plain normalized
    /// anchor cannot, because the UI canvas and the world background are scaled independently).
    ///
    /// Each booster's icon is baked into its own disc sprite (no separate icon child), so we never
    /// hide the button image — we only move + size it. The "20" cost badge is a child and rides
    /// along. Runtime-only: deleting this file fully reverts the look.
    /// </summary>
    [DefaultExecutionOrder(250)]
    public class GameHudThemer : MonoBehaviour
    {
        // Booster button object names, left → right, to match the three background circles.
        private static readonly string[] BoosterNames = { "ShuffleButton", "HintButton", "UndoButton" };

        // --- Tunables --------------------------------------------------------------------------
        // Normalized centre of each painted circle inside the background art (0..1, x left→right).
        private static readonly float[] SlotNX = { 0.313f, 0.5f, 0.692f };
        // Normalized height of the circle row inside the art (0 = art bottom, 1 = art top).
        private const float SlotNY = 0.075f;
        // On-screen size of each booster disc (px at the canvas reference resolution).
        private const float SlotSize = 145f;
        // Footer container(s) whose background art (wooden tray) should be hidden.
        private static readonly string[] FooterContainers = { "FooterPanel", "FooterGui" };
        // ---------------------------------------------------------------------------------------

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
            if (FindFirstObjectByType<GameHudThemer>()) return;
            new GameObject(nameof(GameHudThemer)).AddComponent<GameHudThemer>();
        }

        private void Start() => StartCoroutine(ApplyWhenReady());

        private IEnumerator ApplyWhenReady()
        {
            // Give the footer UI and the game board a couple of frames to build.
            yield return null;
            yield return null;

            HideFooterTray();

            // The shell swaps in the emerald background shortly after the board builds, so wait
            // for a real sprite before we measure it, then re-apply once in case it swaps late.
            Apply();
            yield return new WaitForSecondsRealtime(0.4f);
            HideFooterTray();
            Apply();
        }

        private void Apply()
        {
            GameBoard board = FindFirstObjectByType<GameBoard>();
            SpriteRenderer bg = board ? board.backGround : null;
            Camera cam = Camera.main ?? FindFirstObjectByType<Camera>();

            Canvas root = null;
            for (int i = 0; i < BoosterNames.Length; i++)
            {
                GameObject go = GameObject.Find(BoosterNames[i]);
                if (!go) continue;

                if (root == null)
                {
                    Canvas c = go.GetComponentInParent<Canvas>();
                    root = c ? c.rootCanvas : null;
                }

                PlaceInSlot(go, i, root, bg, cam);
            }
        }

        private void PlaceInSlot(GameObject button, int index, Canvas root, SpriteRenderer bg, Camera cam)
        {
            if (!(button.transform is RectTransform rt)) return;

            // Move onto the full-screen root canvas so the position we compute maps to the screen.
            if (root)
            {
                rt.SetParent(root.transform, false);
                rt.SetAsLastSibling();
            }
            rt.sizeDelta = new Vector2(SlotSize, SlotSize);
            rt.localScale = Vector3.one;

            if (TryPlaceOnCircle(rt, index, root, bg, cam)) return;

            // Fallback: treat the slot coordinates as normalized screen anchors.
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(SlotNX[index], SlotNY);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>Projects the painted circle position (inside the background sprite) to the
        /// canvas so the booster lands exactly on it. Returns false if anything is missing.</summary>
        private bool TryPlaceOnCircle(RectTransform rt, int index, Canvas root, SpriteRenderer bg, Camera cam)
        {
            if (!bg || !cam || !root) return false;
            if (!(root.transform is RectTransform canvasRt)) return false;

            Bounds b = bg.bounds;
            Vector3 world = new Vector3(
                b.min.x + SlotNX[index] * b.size.x,
                b.min.y + SlotNY * b.size.y,
                b.center.z);

            Vector3 screen = cam.WorldToScreenPoint(world);
            Camera uiCam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, uiCam, out Vector2 local))
                return false;

            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = local;
            return true;
        }

        private static void HideFooterTray()
        {
            for (int i = 0; i < FooterContainers.Length; i++)
            {
                GameObject go = GameObject.Find(FooterContainers[i]);
                if (!go) continue;
                Image img = go.GetComponent<Image>();
                if (!img) continue;
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }
}
