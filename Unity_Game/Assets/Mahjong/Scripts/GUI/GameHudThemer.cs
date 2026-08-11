using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mkey.Shell;

namespace Mkey
{
    /// <summary>
    /// Places Shuffle / Hint / Undo in an equal-spaced bottom row, vertically centred inside the
    /// wooden FooterPanel (height reduced ~15%), nudged 25px upward, and clear of the Android
    /// bottom safe area. Keeps the decorative wooden frame visible.
    /// </summary>
    [DefaultExecutionOrder(250)]
    public class GameHudThemer : MonoBehaviour
    {
        private static readonly string[] BoosterNames = { "ShuffleButton", "HintButton", "UndoButton" };

        // Equal horizontal slots across the wooden panel.
        private static readonly float[] SlotNX = { 0.25f, 0.50f, 0.75f };
        private const float SlotSize = 120f;
        // Authored FooterPanel height (scene) before the 15% shrink.
        private const float AuthoredPanelH = 248f;
        private const float PanelHeightScale = 0.85f;
        // Extra lift inside the panel so buttons sit slightly above geometric centre.
        private const float InsidePanelNudgeUp = 25f;
        private static readonly string[] TrayOnly = { "LayerButtonsPanel" };

        // Premium emerald booster look (#0FA958 + gold rim + soft gold glow).
        private static readonly Color EmeraldBg = new Color(0.059f, 0.663f, 0.345f, 0.96f); // #0FA958
        private static readonly Color GoldRim = new Color(0.961f, 0.718f, 0f, 0.95f);       // #F5B700
        private static readonly Color GoldIcon = new Color(1f, 0.85f, 0.4f, 1f);

        private bool panelSized;

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
            yield return null;
            yield return null;

            float t = 0f;
            while (t < 4f)
            {
                if (MatchIQShellBridge.IsActive)
                {
                    Apply(); // hides boosters + footer
                }
                else
                {
                    HideInnerTrayOnly();
                    SizeAndLiftWoodenPanel();
                    Apply();
                }
                yield return new WaitForSecondsRealtime(0.25f);
                t += 0.25f;
            }

            while (true)
            {
                if (MatchIQShellBridge.IsActive)
                {
                    Apply();
                }
                else if (BoostersInTray())
                {
                    HideInnerTrayOnly();
                    SizeAndLiftWoodenPanel();
                    Apply();
                }
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private static bool BoostersInTray()
        {
            for (int i = 0; i < BoosterNames.Length; i++)
            {
                GameObject go = GameObject.Find(BoosterNames[i]);
                if (!go || !go.transform.parent) continue;
                string parent = go.transform.parent.name;
                if (parent == "LayerButtonsPanel" || parent == "FooterPanel" || parent == "FooterGui")
                    return true;
            }
            return false;
        }

        private void Apply()
        {
            // React Native owns Hint / Shuffle / Undo — mute Unity chrome, keep helpers alive.
            if (MatchIQShellBridge.IsActive)
            {
                HideShellOwnedBottomChrome();
                return;
            }

            Canvas root = null;
            float panelH = AuthoredPanelH * PanelHeightScale;
            float bottomInset = 0f;

            for (int i = 0; i < BoosterNames.Length; i++)
            {
                GameObject go = GameObject.Find(BoosterNames[i]);
                if (!go) continue;
                if (root == null)
                {
                    Canvas c = go.GetComponentInParent<Canvas>();
                    root = c ? c.rootCanvas : null;
                    bottomInset = root ? WxoSafeArea.BottomInsetCanvas(root) : 0f;
                }
                PlaceBottomButton(go, i, root, bottomInset, panelH);
                StyleEmeraldBooster(go);
            }
        }

        private static void HideShellOwnedBottomChrome()
        {
            for (int i = 0; i < BoosterNames.Length; i++)
                HideNamedIncludingInactive(BoosterNames[i]);
            HideNamedIncludingInactive("FooterPanel");
            HideNamedIncludingInactive("FooterGui");
            HideNamedIncludingInactive("LayerButtonsPanel");
        }

        private static void HideNamedIncludingInactive(string name)
        {
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (!t || t.name != name) continue;
                if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded) continue;
                MatchIQShellHudHider.HideVisuals(t.gameObject);
            }
        }

        /// <summary>
        /// Premium emerald-green circular booster: #0FA958 fill, gold border, gold child icons,
        /// soft outer glow. Size is applied by <see cref="PlaceBottomButton"/>.
        /// </summary>
        private static void StyleEmeraldBooster(GameObject button)
        {
            Image img = button.GetComponent<Image>();
            if (img)
            {
                img.color = EmeraldBg;
                img.raycastTarget = true;
            }

            Outline outline = button.GetComponent<Outline>();
            if (!outline) outline = button.AddComponent<Outline>();
            outline.effectColor = GoldRim;
            outline.effectDistance = new Vector2(2.2f, -2.2f);

            Shadow glow = button.GetComponent<Shadow>();
            if (!glow) glow = button.AddComponent<Shadow>();
            glow.effectColor = new Color(GoldRim.r, GoldRim.g, GoldRim.b, 0.45f);
            glow.effectDistance = new Vector2(0f, -3f);

            // Tint any nested icon / label gold so the glyph reads premium on emerald.
            foreach (Graphic g in button.GetComponentsInChildren<Graphic>(true))
            {
                if (!g || g.gameObject == button) continue;
                if (g is Text || g is Image)
                    g.color = GoldIcon;
            }
        }

        /// <summary>
        /// Bottom-anchored equal spacing. Y = safe-area + vertical centre of the (shrunken) wooden
        /// panel + 25px nudge up — buttons stay fully inside the frame.
        /// </summary>
        private static void PlaceBottomButton(
            GameObject button, int index, Canvas root, float bottomInset, float panelH)
        {
            if (!(button.transform is RectTransform rt)) return;

            if (root)
            {
                rt.SetParent(root.transform, false);
                rt.SetAsLastSibling();
            }

            // Responsive: scale slightly down on very narrow canvases so 3×120 never clips.
            float canvasW = Screen.width;
            if (root && root.transform is RectTransform rootRt && rootRt.rect.width > 1f)
                canvasW = rootRt.rect.width;
            float size = SlotSize;
            if (canvasW < 980f)
                size = Mathf.Clamp(canvasW * 0.12f, 96f, SlotSize);

            float centerY = bottomInset + panelH * 0.5f + InsidePanelNudgeUp;
            rt.anchorMin = rt.anchorMax = new Vector2(SlotNX[index], 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.localScale = Vector3.one;
            rt.anchoredPosition = new Vector2(0f, centerY);
        }

        private static void HideInnerTrayOnly()
        {
            for (int i = 0; i < TrayOnly.Length; i++)
            {
                GameObject go = GameObject.Find(TrayOnly[i]);
                if (!go) continue;
                Image img = go.GetComponent<Image>();
                if (img)
                {
                    img.color = new Color(1f, 1f, 1f, 0f);
                    img.raycastTarget = false;
                }
                var layout = go.GetComponent<HorizontalLayoutGroup>();
                if (layout) layout.enabled = false;
            }
        }

        /// <summary>
        /// Shrinks FooterPanel height by ~15%, restores its wood/gold look, and lifts it by the
        /// bottom safe-area inset so the decorative frame stays fully on-screen.
        /// </summary>
        private void SizeAndLiftWoodenPanel()
        {
            string[] frames = { "FooterPanel", "FooterGui" };
            for (int i = 0; i < frames.Length; i++)
            {
                GameObject go = GameObject.Find(frames[i]);
                if (!go || !(go.transform is RectTransform rt)) continue;

                Image img = go.GetComponent<Image>();
                if (img)
                {
                    if (img.color.a < 0.05f && img.sprite != null)
                        img.color = Color.white;
                    img.raycastTarget = false;
                }

                Canvas c = go.GetComponentInParent<Canvas>();
                Canvas root = c ? c.rootCanvas : null;
                float bottomInset = root ? WxoSafeArea.BottomInsetCanvas(root) : 0f;

                // FooterPanel is bottom-pivoted with a fixed sizeDelta height — shrink once.
                if (go.name == "FooterPanel")
                {
                    Vector2 size = rt.sizeDelta;
                    float targetH = AuthoredPanelH * PanelHeightScale;
                    if (!panelSized || !Mathf.Approximately(size.y, targetH))
                    {
                        size.y = targetH;
                        rt.sizeDelta = size;
                        panelSized = true;
                    }
                    // Keep width responsive: stretch-ish via anchors already bottom-centred.
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, bottomInset);
                }
                else
                {
                    // FooterGui: raise above the gesture bar if bottom-anchored / stretched.
                    if (Mathf.Approximately(rt.anchorMin.y, 0f) && Mathf.Approximately(rt.anchorMax.y, 0f))
                    {
                        Vector2 pos = rt.anchoredPosition;
                        pos.y = Mathf.Max(pos.y, bottomInset);
                        rt.anchoredPosition = pos;
                    }
                    else
                    {
                        Vector2 min = rt.offsetMin;
                        min.y = Mathf.Max(min.y, bottomInset);
                        rt.offsetMin = min;
                    }
                }
            }
        }
    }
}
