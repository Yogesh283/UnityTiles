using UnityEngine;
using UnityEngine.UI;
using Mkey.Shell;

namespace Mkey
{
    /// <summary>
    /// Countdown shown as a single labelled "TIME" panel centred in the play header (top nav).
    /// LEVEL / SCORE / MATCHES stay as plain text on the banner (no code-drawn panels) and are
    /// shifted left/right so nothing overlaps the centre TIME panel.
    /// </summary>
    public class CampaignTimerHud : MonoBehaviour
    {
        private static CampaignTimerHud instance;

        private Text timerText;
        private bool expiredHandled;
        private bool headerArranged;
        private System.Action onExpired;

        // Safe-area: cached root canvas + the last applied top inset / screen size so we can push the
        // whole header row down under the status bar / notch and re-apply on rotation.
        private Canvas rootCanvas;
        private float appliedTop = float.NaN;
        private int safeLastW = -1;
        private int safeLastH = -1;
        private bool shellVisualsHidden;

        // Original header positions (restored on hide)
        private RectTransform levelRt;
        private RectTransform scoreRt;
        private RectTransform matchesRt;
        private RectTransform menuRt;
        private Vector2 levelOrig;
        private Vector2 scoreOrig;
        private Vector2 matchesOrig;
        private Vector2 menuOrig;

        public static CampaignTimerHud Ensure(System.Action onExpired = null)
        {
            if (instance)
            {
                instance.onExpired = onExpired;
                instance.expiredHandled = false;
                instance.ArrangeHeader();
                instance.ApplyLayout();
                return instance;
            }

            Transform parent = ResolveParent();
            if (!parent) return null;

            GameObject root = new GameObject("CampaignTimerHud", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            instance = root.AddComponent<CampaignTimerHud>();
            instance.onExpired = onExpired;
            instance.Build(root.GetComponent<RectTransform>());
            instance.ArrangeHeader();
            return instance;
        }

        public static void Hide()
        {
            if (!instance) return;
            instance.RestoreHeader();
            Destroy(instance.gameObject);
            instance = null;
        }

        private void OnDestroy()
        {
            RestoreHeader();
            if (instance == this) instance = null;
        }

        private void Build(RectTransform rt)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // TIME section — uses the shared 9-slice panel sprite (dark + gold rim) so it scales
            // crisply on any mobile size, matching the LEVEL / SCORE / MATCHES sections.
            GameObject bgGo = new GameObject("Bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(rt, false);
            Image bg = bgGo.GetComponent<Image>();
            bg.raycastTarget = false;
            Stretch(bg.rectTransform);
            Sprite panel = PanelSprite();
            if (panel)
            {
                bg.sprite = panel;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.05f, 0.05f, 0.07f, 0.92f);
                Outline rim = bgGo.AddComponent<Outline>();
                rim.effectColor = new Color(0.85f, 0.7f, 0.3f, 0.7f);
                rim.effectDistance = new Vector2(1.4f, -1.4f);
            }

            // "TIME" caption — top band of the equal-height section.
            GameObject capGo = new GameObject("Caption", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            capGo.transform.SetParent(rt, false);
            Text caption = capGo.GetComponent<Text>();
            caption.font = font;
            caption.text = "TIME";
            caption.fontSize = 22;
            caption.fontStyle = FontStyle.Bold;
            caption.color = new Color(1f, 0.85f, 0.4f, 0.85f);
            caption.alignment = TextAnchor.MiddleCenter;
            caption.raycastTarget = false;
            RectTransform capRt = caption.rectTransform;
            capRt.anchorMin = new Vector2(0f, 0.52f);
            capRt.anchorMax = new Vector2(1f, 1f);
            capRt.pivot = new Vector2(0.5f, 0.5f);
            capRt.offsetMin = new Vector2(6f, 0f);
            capRt.offsetMax = new Vector2(-6f, -4f);

            // Countdown value — bottom band, vertically centred in its half.
            GameObject timerGo = new GameObject("Timer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            timerGo.transform.SetParent(rt, false);
            timerText = timerGo.GetComponent<Text>();
            timerText.font = font;
            timerText.text = CampaignLevelTimer.FormatRemaining();
            timerText.fontSize = 40;
            timerText.fontStyle = FontStyle.Bold;
            timerText.color = NormalColor;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
            timerText.verticalOverflow = VerticalWrapMode.Overflow;
            timerText.raycastTarget = false;
            RectTransform tRt = timerText.rectTransform;
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 0.50f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.offsetMin = new Vector2(6f, 6f);
            tRt.offsetMax = new Vector2(-6f, 0f);
            Shadow glow = timerGo.AddComponent<Shadow>();
            glow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            glow.effectDistance = new Vector2(1f, -1.5f);

            ApplyLayout();
            SyncShellVisibility();
        }

        // WXO gold — warm gold digits that read well on the dark badge / red-gold banner.
        private static readonly Color NormalColor = new Color(1f, 0.85f, 0.4f, 1f);
        // WXO brand red (#E31C23) — used for the final countdown seconds.
        private static readonly Color WxoRed = new Color(0.89f, 0.11f, 0.14f, 1f);

        // Shared vertical centre of the header row (canvas units, top-anchored) + safe-area inset.
        private const float HeaderY = -148f;

        // Equal-width sections: LEVEL | SCORE | TIME | MATCHES. TIME stays at X=0.
        // LevelX chosen so LEVEL clears the compact Leave button on 1080×2400 / 1220×2584.
        private const float SectionW = 150f;
        private const float SectionH = 104f;
        private const float LevelX = -305f;
        private const float ScoreX = -160f;
        private const float MatchesX = 160f;
        private const float MenuX = 320f;

        private static readonly Vector2 StatSize = new Vector2(SectionW, SectionH);
        // Menu icon matches Leave height (~68), vertically centred on HeaderY with TIME.
        private const float MenuSize = 68f;

        private const int AutoSizeMin = 15;
        private const int AutoSizeMax = 32;

        private void ApplyLayout()
        {
            RectTransform rt = transform as RectTransform;
            if (!rt) return;

            // Centered TIME section sitting on the header bar, LEVEL/SCORE to its left and
            // MATCHES to its right (see ArrangeHeader). Pushed down by the safe-area top inset so it
            // clears the status bar / notch.
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, HeaderY - TopOffset());
            // Same width as LEVEL / SCORE / MATCHES for a balanced equal-width HUD row.
            rt.sizeDelta = new Vector2(SectionW, SectionH);
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
        }

        // Safe-area top inset in canvas units. 0 on devices without a notch / status-bar inset (and
        // in the Editor), so the current layout is unchanged there.
        private Canvas RootCanvas()
        {
            if (!rootCanvas)
            {
                Canvas c = GetComponentInParent<Canvas>();
                rootCanvas = c ? c.rootCanvas : null;
            }
            return rootCanvas;
        }

        private float TopOffset() => WxoSafeArea.TopInsetCanvas(RootCanvas());

        // Places LEVEL / SCORE / MATCHES / menu and the timer badge on a single row, pushed down by
        // the safe-area top inset. Uses the fixed X slots (not the captured originals) so it can be
        // re-run whenever the inset or screen size changes without drifting.
        private void ApplyHeaderRow(float top)
        {
            float y = HeaderY - top;
            PlaceStat(levelRt, LevelX, y);
            PlaceStat(scoreRt, ScoreX, y);
            PlaceStat(matchesRt, MatchesX, y);
            if (menuRt)
            {
                // React Native owns Pause/Settings when the shell is active.
                if (MatchIQShellBridge.IsActive)
                {
                    MuteHudRect(menuRt);
                }
                else
                {
                    menuRt.gameObject.SetActive(true);
                    menuRt.anchorMin = menuRt.anchorMax = new Vector2(0.5f, 1f);
                    menuRt.pivot = new Vector2(0.5f, 0.5f);
                    menuRt.anchoredPosition = new Vector2(MenuX, y);
                    menuRt.sizeDelta = new Vector2(MenuSize, MenuSize);
                }
            }

            // When shell owns HUD, don't leave LEVEL/SCORE/MATCHES/TIME drawing under RN.
            if (MatchIQShellBridge.IsActive)
            {
                MuteHudRect(levelRt);
                MuteHudRect(scoreRt);
                MuteHudRect(matchesRt);
            }

            // TIME stays perfectly centred on X = 0.
            if (transform is RectTransform rt) rt.anchoredPosition = new Vector2(0f, y);

            PolishHeaderText();

            appliedTop = top;
            safeLastW = Screen.width;
            safeLastH = Screen.height;
            SyncShellVisibility();
        }

        /// <summary>
        /// When React Native owns the HUD, keep this component alive for expiry callbacks but hide
        /// every graphic (TIME badge + LEVEL/SCORE/MATCHES/menu) so nothing double-draws under RN.
        /// </summary>
        private void SyncShellVisibility()
        {
            bool hide = MatchIQShellBridge.IsActive;
            if (!hide)
            {
                if (!shellVisualsHidden) return;
                shellVisualsHidden = false;
                SetSubtreeGraphicsEnabled(transform, true);
                return;
            }

            // Always re-apply while shell-active — header polish / themer may re-enable Graphics.
            shellVisualsHidden = true;
            SetSubtreeGraphicsEnabled(transform, false);
            MuteHudRect(levelRt);
            MuteHudRect(scoreRt);
            MuteHudRect(matchesRt);
            MuteHudRect(menuRt);
            MatchIQShellHudHider.HideVisuals(gameObject);
        }

        private static void MuteHudRect(RectTransform rt)
        {
            if (!rt) return;
            MatchIQShellHudHider.HideVisuals(rt.gameObject);
        }

        private static void SetSubtreeGraphicsEnabled(Transform root, bool enabled)
        {
            if (!root) return;
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (!graphics[i]) continue;
                graphics[i].enabled = enabled;
                if (!enabled) graphics[i].raycastTarget = false;
            }
            CanvasGroup cg = root.GetComponent<CanvasGroup>();
            if (!enabled)
            {
                if (!cg) cg = root.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
            else if (cg)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        private static void PlaceStat(RectTransform rt, float x, float y)
        {
            if (!rt) return;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = StatSize;
            rt.localScale = Vector3.one;
        }

        private void ArrangeHeader()
        {
            if (headerArranged) return;
            if (!HeaderGUIController.Instance) return;

            Transform header = HeaderGUIController.Instance.transform;
            levelRt = header.Find("Level") as RectTransform;
            scoreRt = header.Find("ScoreCounter") as RectTransform;
            matchesRt = header.Find("PossibleMatchesCounter") as RectTransform;
            menuRt = header.Find("ButtonMenu") as RectTransform;

            if (levelRt) levelOrig = levelRt.anchoredPosition;
            if (scoreRt) scoreOrig = scoreRt.anchoredPosition;
            if (matchesRt) matchesOrig = matchesRt.anchoredPosition;
            if (menuRt) menuOrig = menuRt.anchoredPosition;

            headerArranged = true;
            ApplyHeaderRow(TopOffset());
            PolishHeaderText();
        }

        // Stacks the caption above the value inside each LEVEL / SCORE / MATCHES section so they
        // never overlap (the old "stretch both texts to fill parent" path caused the overlap).
        private void PolishHeaderText()
        {
            StackStatTexts(levelRt);
            StackStatTexts(scoreRt);
            StackStatTexts(matchesRt);
        }

        private static void StackStatTexts(RectTransform container)
        {
            if (!container) return;
            Text[] texts = container.GetComponentsInChildren<Text>(true);
            if (texts == null || texts.Length == 0) return;

            Text label = null;
            Text value = null;
            for (int i = 0; i < texts.Length; i++)
            {
                Text t = texts[i];
                if (!t) continue;
                string n = t.gameObject.name;
                // Scene names: LevelText / ScoreText / MatchesText = labels;
                // CounterText / ScoreCounterText = values.
                bool looksLikeValue =
                    n.IndexOf("Counter", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    IsMostlyDigits(t.text);
                if (looksLikeValue) value = t;
                else if (label == null) label = t;
            }

            if (label == null && texts.Length > 0) label = texts[0];
            if (value == null && texts.Length > 1) value = texts[texts.Length - 1];
            if (value == null) value = label;

            if (label && label != value)
                PlaceTextBand(label, 0.52f, 1f, 4f, -2f, AutoSizeMin, 26);
            if (value)
                PlaceTextBand(value, 0f, label && label != value ? 0.50f : 1f, 2f, -4f, AutoSizeMin, AutoSizeMax);
        }

        private static bool IsMostlyDigits(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            int digits = 0, other = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsDigit(c)) digits++;
                else if (!char.IsWhiteSpace(c)) other++;
            }
            return digits > 0 && digits >= other;
        }

        private static void PlaceTextBand(
            Text t, float anchorMinY, float anchorMaxY,
            float padBottom, float padTop, int minSize, int maxSize)
        {
            RectTransform tr = t.rectTransform;
            tr.anchorMin = new Vector2(0f, anchorMinY);
            tr.anchorMax = new Vector2(1f, anchorMaxY);
            tr.pivot = new Vector2(0.5f, 0.5f);
            tr.offsetMin = new Vector2(4f, padBottom);
            tr.offsetMax = new Vector2(-4f, padTop);
            t.alignment = TextAnchor.MiddleCenter;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = minSize;
            t.resizeTextMaxSize = maxSize;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
        }

        private void RestoreHeader()
        {
            if (!headerArranged) return;
            if (levelRt) levelRt.anchoredPosition = levelOrig;
            if (scoreRt) scoreRt.anchoredPosition = scoreOrig;
            if (matchesRt) matchesRt.anchoredPosition = matchesOrig;
            if (menuRt) menuRt.anchoredPosition = menuOrig;

            headerArranged = false;
        }

        private void Update()
        {
            if (!timerText) return;

            // Keep the header clear of the status bar / notch after rotation, fold, or a late safe-area
            // report. Only touches transforms when something actually changed.
            if (headerArranged)
            {
                float top = TopOffset();
                if (Screen.width != safeLastW || Screen.height != safeLastH || !Mathf.Approximately(top, appliedTop))
                {
                    ApplyHeaderRow(top);
                    ApplyLayout();
                }
            }

            float remaining = CampaignLevelTimer.RemainingSeconds;
            timerText.text = CampaignLevelTimer.FormatRemaining();

            if (remaining <= 10f)
                timerText.color = WxoRed;
            else if (remaining <= 30f)
                timerText.color = new Color(1f, 0.75f, 0.25f, 1f);
            else
                timerText.color = NormalColor;

            // Keep shell visibility in sync if IsActive flips mid-match.
            SyncShellVisibility();

            if (expiredHandled || !CampaignLevelTimer.IsRunning) return;
            if (remaining > 0f) return;

            expiredHandled = true;
            CampaignLevelTimer.Stop();
            onExpired?.Invoke();
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        // Shared responsive 9-slice panel sprite (Resources/WXO/header_panel) used for every
        // header section. Cached so it loads once.
        private static Sprite panelSprite;
        private static bool panelLoaded;
        private static Sprite PanelSprite()
        {
            if (!panelLoaded)
            {
                panelSprite = Resources.Load<Sprite>("WXO/header_panel");
                panelLoaded = true;
            }
            return panelSprite;
        }

        private static Transform ResolveParent()
        {
            if (HeaderGUIController.Instance)
                return HeaderGUIController.Instance.transform;

            GameObject canvasMain = GameObject.Find("CanvasMain");
            if (canvasMain) return canvasMain.transform;

            Canvas c = Object.FindFirstObjectByType<Canvas>();
            return c ? c.transform : null;
        }
    }
}
