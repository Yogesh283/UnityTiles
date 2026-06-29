using System;
using Mkey;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    public static class TournamentResultDialog
    {
        public static bool IsVisible => TournamentResultOverlayHost.Instance != null && TournamentResultOverlayHost.Instance.IsShowing;

        public static void ShowDuelWin(int prizeCoins, Action onClosed)
        {
            Show(
                "🏆 YOU WIN!",
                $"Fastest finish!\n\nReward: {prizeCoins:N0} Coins\nRank: #1",
                onClosed);
        }

        public static void ShowDuelLoss(Action onClosed)
        {
            Show(
                "❌ GAME OVER",
                "Your opponent finished faster.\n\nBetter Luck Next Time.\n\nReward: 0 Coins",
                onClosed);
        }

        public static void ShowDuelWaiting()
        {
            TournamentResultOverlayHost.EnsureInstance().PresentWaiting(
                "⏳ WAITING",
                "You finished the level.\n\nWaiting for your opponent...");
        }

        public static void HideWaitingIfVisible()
        {
            if (TournamentResultOverlayHost.Instance != null)
                TournamentResultOverlayHost.Instance.HideWaiting();
        }

        public static void ShowRankWin(int rank, int prizeCoins, Action onClosed)
        {
            Show(
                "CONGRATULATIONS!",
                $"Rank: #{rank:N0}\n\nPrize Won: {prizeCoins:N0} Coins\nCoins Added to Wallet",
                onClosed);
        }

        public static void ShowRankLoss(string tournamentId, int rank, Action onClosed)
        {
            string extra = tournamentId == "quick_cup" && rank >= 4 && rank <= 10
                ? "Thank you for participating.\n\n"
                : "Better Luck Next Time\n\n";

            Show(
                "GAME OVER",
                $"{extra}Your Rank: #{rank:N0}\nPrize: 0 Coins",
                onClosed);
        }

        private static void Show(string title, string message, Action onClosed)
        {
            TournamentResultOverlayHost.EnsureInstance().Present(title, message, onClosed);
        }

        public static void ReturnToTournamentPage()
        {
            TournamentMatchManager.DestroyRoom();
            TournamentSession.Clear();

            if (SceneLoader.Instance)
                SceneLoader.Instance.LoadScene(TournamentSession.TournamentSceneIndex);
            else
                SceneManager.LoadScene(TournamentSession.TournamentSceneIndex);
        }
    }

    /// <summary>
    /// Self-contained tournament result popup (works without GuiController — both winner and loser devices).
    /// </summary>
    internal class TournamentResultOverlayHost : MonoBehaviour
    {
        private const int SortOrder = 5000;

        public static TournamentResultOverlayHost Instance { get; private set; }

        public bool IsShowing { get; private set; }

        private bool isWaitingMode;

        private RectTransform overlayRoot;
        private RectTransform panel;
        private Text titleText;
        private Text messageText;
        private Button okButton;
        private Action pendingCloseAction;

        public static TournamentResultOverlayHost EnsureInstance()
        {
            if (Instance) return Instance;

            GameObject host = new GameObject(nameof(TournamentResultOverlayHost));
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<TournamentResultOverlayHost>();
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

        public void Present(string title, string message, Action onClosed)
        {
            EnsureEventSystem();
            isWaitingMode = false;
            if (okButton) okButton.gameObject.SetActive(true);
            pendingCloseAction = onClosed;
            titleText.text = title;
            messageText.text = message;
            overlayRoot.gameObject.SetActive(true);
            overlayRoot.SetAsLastSibling();
            IsShowing = true;

            panel.localScale = Vector3.one * 0.92f;
            SimpleTween.Cancel(gameObject, false);
            SimpleTween.Value(gameObject, 0f, 1f, 0.22f)
                .SetEase(EaseAnim.EaseOutBack)
                .SetOnUpdate(t => panel.localScale = Vector3.LerpUnclamped(Vector3.one * 0.92f, Vector3.one, t));
        }

        public void PresentWaiting(string title, string message)
        {
            EnsureEventSystem();
            isWaitingMode = true;
            pendingCloseAction = null;
            if (okButton) okButton.gameObject.SetActive(false);
            titleText.text = title;
            messageText.text = message;
            overlayRoot.gameObject.SetActive(true);
            overlayRoot.SetAsLastSibling();
            IsShowing = true;
            panel.localScale = Vector3.one;
        }

        public void HideWaiting()
        {
            if (!isWaitingMode) return;
            isWaitingMode = false;
            IsShowing = false;
            overlayRoot.gameObject.SetActive(false);
            if (okButton) okButton.gameObject.SetActive(true);
        }

        private void OnOkClicked()
        {
            if (!IsShowing || isWaitingMode) return;

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
            Image dim = TournamentUIFactory.CreateImage(overlayRoot, "Dim", new Color(0f, 0f, 0f, 0.82f));
            TournamentUIFactory.StretchRect(dim.rectTransform);
            dim.raycastTarget = true;

            panel = TournamentUIFactory.CreateRect(overlayRoot, "Panel");
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(TournamentLayoutMetrics.S(820f), TournamentLayoutMetrics.S(580f));
            TournamentPremiumUI.CreateDialogPanel(panel);

            titleText = TournamentUIFactory.CreateText(
                panel, "Title", string.Empty,
                TournamentLayoutMetrics.Font(36f), FontStyle.Bold,
                TournamentPremiumTheme.GoldBright, TextAnchor.UpperCenter);
            titleText.rectTransform.anchorMin = new Vector2(0.06f, 0.72f);
            titleText.rectTransform.anchorMax = new Vector2(0.94f, 0.94f);
            titleText.rectTransform.offsetMin = titleText.rectTransform.offsetMax = Vector2.zero;
            TournamentUIFactory.AddGoldOutline(titleText);

            messageText = TournamentUIFactory.CreateText(
                panel, "Message", string.Empty,
                TournamentLayoutMetrics.Font(26f), FontStyle.Normal,
                TournamentPremiumTheme.TextWhite, TextAnchor.MiddleCenter);
            messageText.rectTransform.anchorMin = new Vector2(0.08f, 0.28f);
            messageText.rectTransform.anchorMax = new Vector2(0.92f, 0.68f);
            messageText.rectTransform.offsetMin = messageText.rectTransform.offsetMax = Vector2.zero;

            RectTransform btnRt = TournamentUIFactory.CreateRect(panel, "OkButton");
            btnRt.anchorMin = new Vector2(0.2f, 0.06f);
            btnRt.anchorMax = new Vector2(0.8f, 0.18f);
            btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;

            Image btnBg = TournamentUIFactory.CreateSlicedImage(btnRt, "Bg", Color.white, TournamentSpriteFactory.ButtonGreen, true);
            TournamentUIFactory.StretchRect(btnBg.rectTransform);

            okButton = btnRt.gameObject.AddComponent<Button>();
            okButton.targetGraphic = btnBg;
            okButton.onClick.AddListener(OnOkClicked);

            Text btnLabel = TournamentUIFactory.CreateText(
                btnRt, "Label", "OK",
                TournamentLayoutMetrics.Font(24f), FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter);
            TournamentUIFactory.StretchRect(btnLabel.rectTransform);

            overlayRoot.gameObject.SetActive(false);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current) return;

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(eventSystem);
        }
    }
}
