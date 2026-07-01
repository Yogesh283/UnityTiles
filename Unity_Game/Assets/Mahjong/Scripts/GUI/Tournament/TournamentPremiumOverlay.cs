using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Global full-screen overlay using the same premium panel style as the waiting room.
    /// </summary>
    public static class TournamentPremiumOverlay
    {
        private const float DefaultAutoReturnSeconds = 3f;

        public static bool IsVisible =>
            TournamentPremiumOverlayHost.Instance != null && TournamentPremiumOverlayHost.Instance.IsShowing;

        public static void ForceDismiss()
        {
            if (TournamentPremiumOverlayHost.Instance != null)
                TournamentPremiumOverlayHost.Instance.Dismiss();
        }

        public static void Show(
            string title,
            string subtitle,
            string highlight,
            string body,
            string footer = null,
            Action onClosed = null,
            float autoReturnSeconds = DefaultAutoReturnSeconds)
        {
            TournamentPremiumOverlayHost.EnsureInstance().Present(
                title,
                subtitle,
                highlight,
                body,
                footer,
                onClosed,
                autoReturnSeconds);
        }
    }

    internal class TournamentPremiumOverlayHost : MonoBehaviour
    {
        private const int SortOrder = 4600;

        public static TournamentPremiumOverlayHost Instance { get; private set; }

        public bool IsShowing { get; private set; }

        private RectTransform overlayRoot;
        private RectTransform panel;
        private Text titleText;
        private Text subtitleText;
        private Text bodyText;
        private Text highlightText;
        private Text footerText;

        private Action pendingCloseAction;
        private Coroutine countdownRoutine;
        private string baseFooter;

        public static TournamentPremiumOverlayHost EnsureInstance()
        {
            if (Instance) return Instance;

            GameObject host = new GameObject(nameof(TournamentPremiumOverlayHost));
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<TournamentPremiumOverlayHost>();
            Instance.BuildUi();
            return Instance;
        }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (!overlayRoot)
                BuildUi();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Present(
            string title,
            string subtitle,
            string highlight,
            string body,
            string footer,
            Action onClosed,
            float autoReturnSeconds)
        {
            UiEventSystemGuard.EnforceSingle();
            pendingCloseAction = onClosed;
            baseFooter = footer ?? string.Empty;

            titleText.text = title ?? string.Empty;
            subtitleText.text = subtitle ?? string.Empty;
            highlightText.text = highlight ?? string.Empty;
            bodyText.text = body ?? string.Empty;
            footerText.text = baseFooter;

            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
            highlightText.gameObject.SetActive(!string.IsNullOrEmpty(highlight));
            bodyText.gameObject.SetActive(!string.IsNullOrEmpty(body));

            overlayRoot.gameObject.SetActive(true);
            overlayRoot.SetAsLastSibling();
            IsShowing = true;

            panel.localScale = Vector3.one * 0.9f;
            SimpleTween.Cancel(gameObject, false);
            SimpleTween.Value(gameObject, 0f, 1f, 0.22f)
                .SetEase(EaseAnim.EaseOutBack)
                .SetOnUpdate(t => panel.localScale = Vector3.LerpUnclamped(Vector3.one * 0.9f, Vector3.one, t));

            if (countdownRoutine != null)
                StopCoroutine(countdownRoutine);

            if (autoReturnSeconds > 0f)
                countdownRoutine = StartCoroutine(AutoReturnCountdown(autoReturnSeconds));
        }

        private IEnumerator AutoReturnCountdown(float seconds)
        {
            int remaining = Mathf.CeilToInt(seconds);
            while (remaining > 0)
            {
                footerText.text = string.IsNullOrEmpty(baseFooter)
                    ? $"Returning in {remaining}..."
                    : baseFooter + "\nReturning in " + remaining + "...";
                yield return new WaitForSecondsRealtime(1f);
                remaining--;
            }

            footerText.text = "Returning to Tournament...";
            yield return new WaitForSecondsRealtime(0.15f);
            CompleteAndClose();
        }

        public void Dismiss() => CompleteAndClose();

        private void CompleteAndClose()
        {
            if (!IsShowing) return;

            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
                countdownRoutine = null;
            }

            IsShowing = false;
            overlayRoot.gameObject.SetActive(false);

            Action callback = pendingCloseAction;
            pendingCloseAction = null;
            callback?.Invoke();
        }

        private void BuildUi()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortOrder;
            gameObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            overlayRoot = TournamentUIFactory.CreateRect(transform, "Overlay");
            TournamentUIFactory.StretchRect(overlayRoot);
            Image dim = TournamentUIFactory.CreateImage(overlayRoot, "Dim", new Color(0f, 0f, 0f, 0.72f));
            TournamentUIFactory.StretchRect(dim.rectTransform);
            dim.raycastTarget = true;

            panel = TournamentUIFactory.CreateRect(overlayRoot, "Panel");
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(TournamentLayoutMetrics.S(820f), TournamentLayoutMetrics.S(640f));
            TournamentPremiumUI.CreateDialogPanel(panel);

            titleText = TournamentUIFactory.CreateText(
                panel, "Title", string.Empty,
                TournamentLayoutMetrics.Font(38f), FontStyle.Bold,
                TournamentPremiumTheme.GoldBright, TextAnchor.UpperCenter);
            titleText.rectTransform.anchorMin = new Vector2(0f, 0.78f);
            titleText.rectTransform.anchorMax = new Vector2(1f, 0.95f);
            titleText.rectTransform.offsetMin = titleText.rectTransform.offsetMax = Vector2.zero;
            TournamentUIFactory.AddGoldOutline(titleText);

            subtitleText = TournamentUIFactory.CreateText(
                panel, "Subtitle", string.Empty,
                TournamentLayoutMetrics.Font(28f), FontStyle.Bold,
                TournamentPremiumTheme.TextSoft, TextAnchor.UpperCenter);
            subtitleText.rectTransform.anchorMin = new Vector2(0f, 0.66f);
            subtitleText.rectTransform.anchorMax = new Vector2(1f, 0.78f);
            subtitleText.rectTransform.offsetMin = subtitleText.rectTransform.offsetMax = Vector2.zero;

            bodyText = TournamentUIFactory.CreateText(
                panel, "Body", string.Empty,
                TournamentLayoutMetrics.Font(26f), FontStyle.Normal,
                TournamentPremiumTheme.TextWhite, TextAnchor.MiddleCenter);
            bodyText.rectTransform.anchorMin = new Vector2(0.1f, 0.48f);
            bodyText.rectTransform.anchorMax = new Vector2(0.9f, 0.64f);
            bodyText.rectTransform.offsetMin = bodyText.rectTransform.offsetMax = Vector2.zero;

            highlightText = TournamentUIFactory.CreateText(
                panel, "Highlight", string.Empty,
                TournamentLayoutMetrics.Font(50f), FontStyle.Bold,
                TournamentPremiumTheme.GoldBright, TextAnchor.MiddleCenter);
            highlightText.rectTransform.anchorMin = new Vector2(0.1f, 0.14f);
            highlightText.rectTransform.anchorMax = new Vector2(0.9f, 0.46f);
            highlightText.rectTransform.offsetMin = highlightText.rectTransform.offsetMax = Vector2.zero;

            footerText = TournamentUIFactory.CreateText(
                panel, "Footer", string.Empty,
                TournamentLayoutMetrics.Font(26f), FontStyle.Italic,
                TournamentPremiumTheme.TextMuted, TextAnchor.LowerCenter);
            footerText.rectTransform.anchorMin = new Vector2(0.1f, 0.04f);
            footerText.rectTransform.anchorMax = new Vector2(0.9f, 0.12f);
            footerText.rectTransform.offsetMin = footerText.rectTransform.offsetMax = Vector2.zero;

            overlayRoot.gameObject.SetActive(false);
        }
    }
}
