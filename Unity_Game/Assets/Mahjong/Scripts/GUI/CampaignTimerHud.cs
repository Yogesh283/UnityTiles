using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Compact 5:00 countdown in the play header black pill.
    /// Shifts LEVEL / SCORE / MATCHES so nothing overlaps the timer.
    /// </summary>
    public class CampaignTimerHud : MonoBehaviour
    {
        private static CampaignTimerHud instance;

        private Text timerText;
        private bool expiredHandled;
        private bool headerArranged;
        private System.Action onExpired;

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

            // Gold circular timer badge (Resources/WXO/timer_badge) — matches the emerald frame.
            Sprite badge = Resources.Load<Sprite>("WXO/timer_badge");

            GameObject bgGo = new GameObject("Bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(rt, false);
            Image bg = bgGo.GetComponent<Image>();
            bg.raycastTarget = false;
            Stretch(bg.rectTransform);
            if (badge)
            {
                bg.sprite = badge;
                bg.color = Color.white;
                bg.preserveAspect = true;
            }
            else
            {
                // Fallback to the old dark pill if the badge sprite is unavailable.
                bg.color = new Color(0.02f, 0.02f, 0.04f, 0.95f);
                Outline rim = bgGo.AddComponent<Outline>();
                rim.effectColor = new Color(0.85f, 0.7f, 0.3f, 0.55f);
                rim.effectDistance = new Vector2(1.2f, -1.2f);
            }

            GameObject timerGo = new GameObject("Timer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            timerGo.transform.SetParent(rt, false);
            timerText = timerGo.GetComponent<Text>();
            timerText.font = font;
            timerText.text = CampaignLevelTimer.FormatRemaining();
            timerText.fontSize = 36;
            timerText.fontStyle = FontStyle.Bold;
            timerText.color = NormalColor;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
            timerText.verticalOverflow = VerticalWrapMode.Overflow;
            timerText.raycastTarget = false;
            Stretch(timerText.rectTransform);
            // Drop shadow keeps the gold digits legible on the dark disc.
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
        // Pulled up (smaller magnitude) so the top nav band is shorter and leaves more room for the
        // board on tall phones.
        private const float HeaderY = -54f;

        // Horizontal slots along the header row (0 = centre, under the timer badge). Symmetric
        // layout: LEVEL / SCORE sit left of the timer; MATCHES mirrors SCORE and the menu button
        // mirrors LEVEL so the burger button is pulled in from the frame edge into the banner.
        private const float LevelX = -300f;
        private const float ScoreX = -155f;
        private const float MatchesX = 155f;
        private const float MenuX = 370f;

        private void ApplyLayout()
        {
            RectTransform rt = transform as RectTransform;
            if (!rt) return;

            // Centered circular badge sitting on the header bar, LEVEL/SCORE to its left and
            // MATCHES to its right (see ArrangeHeader).
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, HeaderY);
            rt.sizeDelta = new Vector2(104f, 104f);
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
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

            if (levelRt)
            {
                levelOrig = levelRt.anchoredPosition;
                levelRt.anchoredPosition = new Vector2(LevelX, HeaderY);
            }
            if (scoreRt)
            {
                scoreOrig = scoreRt.anchoredPosition;
                scoreRt.anchoredPosition = new Vector2(ScoreX, HeaderY);
            }
            if (matchesRt)
            {
                matchesOrig = matchesRt.anchoredPosition;
                matchesRt.anchoredPosition = new Vector2(MatchesX, HeaderY);
            }
            if (menuRt)
            {
                menuOrig = menuRt.anchoredPosition;
                menuRt.anchoredPosition = new Vector2(MenuX, HeaderY);
            }

            headerArranged = true;
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
