using UnityEngine;
using UnityEngine.UI;

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

            // "TIME" caption pinned to the top of the section.
            GameObject capGo = new GameObject("Caption", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            capGo.transform.SetParent(rt, false);
            Text caption = capGo.GetComponent<Text>();
            caption.font = font;
            caption.text = "TIME";
            caption.fontSize = 26;
            caption.fontStyle = FontStyle.Bold;
            caption.color = new Color(1f, 0.85f, 0.4f, 0.85f);
            caption.alignment = TextAnchor.MiddleCenter;
            caption.raycastTarget = false;
            RectTransform capRt = caption.rectTransform;
            capRt.anchorMin = new Vector2(0f, 1f);
            capRt.anchorMax = new Vector2(1f, 1f);
            capRt.pivot = new Vector2(0.5f, 1f);
            capRt.offsetMin = new Vector2(10f, -56f);
            capRt.offsetMax = new Vector2(-10f, -14f);

            // Countdown value fills the rest of the section, under the caption.
            GameObject timerGo = new GameObject("Timer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            timerGo.transform.SetParent(rt, false);
            timerText = timerGo.GetComponent<Text>();
            timerText.font = font;
            timerText.text = CampaignLevelTimer.FormatRemaining();
            timerText.fontSize = 52;
            timerText.fontStyle = FontStyle.Bold;
            timerText.color = NormalColor;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
            timerText.verticalOverflow = VerticalWrapMode.Overflow;
            timerText.raycastTarget = false;
            RectTransform tRt = timerText.rectTransform;
            tRt.anchorMin = new Vector2(0f, 0f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.offsetMin = new Vector2(10f, 10f);
            tRt.offsetMax = new Vector2(-10f, -48f);
            // Drop shadow keeps the gold digits legible on the dark panel.
            Shadow glow = timerGo.AddComponent<Shadow>();
            glow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            glow.effectDistance = new Vector2(1f, -1.5f);

            ApplyLayout();
        }

        // WXO gold — warm gold digits that read well on the dark badge / red-gold banner.
        private static readonly Color NormalColor = new Color(1f, 0.85f, 0.4f, 1f);
        // WXO brand red (#E31C23) — used for the final countdown seconds.
        private static readonly Color WxoRed = new Color(0.89f, 0.11f, 0.14f, 1f);

        // Shared vertical position for the whole header row so LEVEL / SCORE / timer / MATCHES /
        // menu all sit inside the emerald frame's top banner. Tune this one value to move the row.
        // Nudged down so the taller top-nav sections sit lower inside the banner nameplate.
        private const float HeaderY = -86f;

        // Horizontal slots along the header row (0 = centre, under the timer badge). Equal spacing of
        // HeaderSpacing between LEVEL → SCORE → TIMER → MATCHES so the row reads evenly; the menu slot
        // mirrors on the right (the burger ButtonMenu = in-game menu; Leave sits on the left).
        // Widened so the top section spreads across the banner and clears the bigger TIME panel.
        private const float HeaderSpacing = 168f;
        private const float LevelX = -2f * HeaderSpacing; // -310
        private const float ScoreX = -HeaderSpacing;      // -155
        private const float MatchesX = HeaderSpacing;     //  155
        private const float MenuX = 2f * HeaderSpacing;   //  310

        // Square size for the right-hand burger menu so it fits the banner row (its authored rect is
        // 136x140, which pokes above the screen top when centred on the row).
        private const float MenuSize = 96f;

        // Legacy-Text "auto size" (the project has no TextMeshPro): every HUD label/value shrinks to
        // fit its own rect between these bounds, so nothing overflows its panel on any aspect ratio.
        private const int AutoSizeMin = 22;
        private const int AutoSizeMax = 40;

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
            rt.sizeDelta = new Vector2(200f, 118f);
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
            if (levelRt) levelRt.anchoredPosition = new Vector2(LevelX, y);
            if (scoreRt) scoreRt.anchoredPosition = new Vector2(ScoreX, y);
            if (matchesRt) matchesRt.anchoredPosition = new Vector2(MatchesX, y);
            if (menuRt)
            {
                // Right-hand in-game menu (burger). Sized to sit inside the banner row and aligned
                // with the timer badge so its large authored rect doesn't clip above the screen.
                menuRt.anchoredPosition = new Vector2(MenuX, y);
                menuRt.sizeDelta = new Vector2(MenuSize, MenuSize);
            }

            if (transform is RectTransform rt) rt.anchoredPosition = new Vector2(0f, y);

            appliedTop = top;
            safeLastW = Screen.width;
            safeLastH = Screen.height;
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

        // Keeps every LEVEL / SCORE / MATCHES label and value inside its panel on all devices:
        // enables legacy best-fit (auto size 22–40) and centres the text both ways. Best-fit scales
        // the font to the rect, so text can never overflow — the fixed-size + Overflow authoring was
        // what let the numbers spill outside the black/gold panels on tall Android screens.
        private void PolishHeaderText()
        {
            ApplyAutoSize(levelRt);
            ApplyAutoSize(scoreRt);
            ApplyAutoSize(matchesRt);
        }

        private static void ApplyAutoSize(RectTransform container)
        {
            if (!container) return;
            Text[] texts = container.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                Text t = texts[i];
                if (!t) continue;
                t.alignment = TextAnchor.MiddleCenter;
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = AutoSizeMin;
                t.resizeTextMaxSize = AutoSizeMax;
                // Best-fit governs the size; keep single-line and clip rather than grow the rect.
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Truncate;
            }
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
