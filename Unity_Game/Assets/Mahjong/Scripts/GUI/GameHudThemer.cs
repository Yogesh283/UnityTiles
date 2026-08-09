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
        // Measured directly from the gold ring glow in Bkg Emerald Temple.png so the discs land
        // dead-centre in each painted ring.
        private static readonly float[] SlotNX = { 0.190f, 0.490f, 0.805f };
        // Normalized height of each circle centre (0 = art bottom, 1 = art top). Tuned empirically on
        // device: pixel-detection over-reads the height because of the ring's upward glow, so this is
        // the on-screen sweet spot that drops the discs into the middle of each ring.
        private static readonly float[] SlotNY = { 0.097f, 0.097f, 0.095f };
        // On-screen size of each booster disc (px at the canvas reference resolution).
        private const float SlotSize = 145f;
        // Footer container(s) whose background art (wooden tray) should be hidden. The boosters live
        // in "LayerButtonsPanel" (a HorizontalLayoutGroup) sitting on the wooden "FooterPanel".
        private static readonly string[] FooterContainers = { "FooterPanel", "FooterGui", "LayerButtonsPanel" };
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

            // The footer tray and background sprite can build/swap a little late, and a level load can
            // rebuild the booster tray after we've themed it. So re-apply for the first few seconds…
            float t = 0f;
            while (t < 4f)
            {
                HideFooterTray();
                Apply();
                yield return new WaitForSecondsRealtime(0.25f);
                t += 0.25f;
            }

            // …then keep a cheap watchdog running: if the boosters ever fall back into the wooden
            // tray (e.g. after a level load rebuilds it), lift them onto the rings again.
            while (true)
            {
                if (BoostersInTray())
                {
                    HideFooterTray();
                    Apply();
                }
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        // True while any booster button is still parented under the wooden tray / layout group,
        // i.e. it has not yet been lifted onto the background's gold rings.
        private static bool BoostersInTray()
        {
            for (int i = 0; i < BoosterNames.Length; i++)
            {
                GameObject go = GameObject.Find(BoosterNames[i]);
                if (!go || !go.transform.parent) continue;
                string parent = go.transform.parent.name;
                for (int f = 0; f < FooterContainers.Length; f++)
                    if (parent == FooterContainers[f]) return true;
            }
            return false;
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

            // Fallback: treat the slot coordinates as normalized screen anchors, lifted above the
            // bottom safe-area inset (gesture / nav bar) so the buttons stay fully tappable.
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(SlotNX[index], SlotNY[index]);
            rt.anchoredPosition = new Vector2(0f, WxoSafeArea.BottomInsetCanvas(root));
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
                b.min.y + SlotNY[index] * b.size.y,
                b.center.z);

            Vector3 screen = cam.WorldToScreenPoint(world);
            Camera uiCam = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, uiCam, out Vector2 local))
                return false;

            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            // Land exactly on the painted ring. No safe-area lift here: the rings are baked into the
            // artwork (already clear of the gesture bar), so adding a device-dependent bottom inset
            // would push the discs up off-centre — which is exactly what made them float above.
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
                // The transparent footer tray was left with raycastTarget = true, so it sat over the
                // lower board and ate tile taps (TouchPadEventArgs drops any collider behind a UI
                // graphic) — tile matching and boosters stopped responding. Only the tray's own image
                // is disabled here; the booster buttons are separate children and stay interactive.
                img.raycastTarget = false;
            }
        }
    }
}
