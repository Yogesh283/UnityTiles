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
        private Vector2 levelOrig;
        private Vector2 scoreOrig;
        private Vector2 matchesOrig;

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

            GameObject bgGo = new GameObject("Bg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(rt, false);
            Image bg = bgGo.GetComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.04f, 0.95f);
            bg.raycastTarget = false;
            Stretch(bg.rectTransform);
            Outline rim = bgGo.AddComponent<Outline>();
            rim.effectColor = new Color(0.85f, 0.7f, 0.3f, 0.55f);
            rim.effectDistance = new Vector2(1.2f, -1.2f);

            GameObject timerGo = new GameObject("Timer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            timerGo.transform.SetParent(rt, false);
            timerText = timerGo.GetComponent<Text>();
            timerText.font = font;
            timerText.text = CampaignLevelTimer.FormatRemaining();
            timerText.fontSize = 30;
            timerText.fontStyle = FontStyle.Bold;
            timerText.color = Color.white;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
            timerText.verticalOverflow = VerticalWrapMode.Overflow;
            timerText.raycastTarget = false;
            Stretch(timerText.rectTransform);

            ApplyLayout();
        }

        private void ApplyLayout()
        {
            RectTransform rt = transform as RectTransform;
            if (!rt) return;

            // Same row as LEVEL / SCORE / MATCHES (y ≈ -100 under HeaderGui)
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(40f, -100f);
            rt.sizeDelta = new Vector2(136f, 48f);
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

            if (levelRt)
            {
                levelOrig = levelRt.anchoredPosition;
                levelRt.anchoredPosition = new Vector2(-300f, -100f);
            }
            if (scoreRt)
            {
                scoreOrig = scoreRt.anchoredPosition;
                scoreRt.anchoredPosition = new Vector2(-155f, -100f);
            }
            if (matchesRt)
            {
                matchesOrig = matchesRt.anchoredPosition;
                matchesRt.anchoredPosition = new Vector2(230f, -100f);
            }

            headerArranged = true;
        }

        private void RestoreHeader()
        {
            if (!headerArranged) return;
            if (levelRt) levelRt.anchoredPosition = levelOrig;
            if (scoreRt) scoreRt.anchoredPosition = scoreOrig;
            if (matchesRt) matchesRt.anchoredPosition = matchesOrig;
            headerArranged = false;
        }

        private void Update()
        {
            if (!timerText) return;

            float remaining = CampaignLevelTimer.RemainingSeconds;
            timerText.text = CampaignLevelTimer.FormatRemaining();

            if (remaining <= 10f)
                timerText.color = new Color(1f, 0.35f, 0.3f, 1f);
            else if (remaining <= 30f)
                timerText.color = new Color(1f, 0.75f, 0.25f, 1f);
            else
                timerText.color = Color.white;

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
