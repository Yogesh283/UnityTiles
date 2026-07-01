using System;
using System.Collections;
using Mkey;
using Mkey.Network;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Tournament screen driven entirely by turnamant1.png with invisible interaction overlays.
    /// </summary>
    public class TournamentPageController : MonoBehaviour
    {
        private const int MapSceneIndex = 1;
        private const int LocalTestWalletCoins = 5000;
        private const string ScriptableHolderResource = "Tournament/ScriptableHolder";

        [SerializeField] private int backSceneIndex = MapSceneIndex;

        private Text walletText;
        private RectTransform pageRoot;
        private RectTransform scrollContent;
        private RectTransform overlayRoot;
        private TournamentDialog dialog;
        private TournamentWaitingRoomPanel waitingRoom;
        private TournamentWalletPulse walletPulse;
        private Text onlineStatusText;
        private bool pageBuilt;

        private void Awake()
        {
            TournamentLayoutMetrics.Refresh();
            UiEventSystemGuard.EnforceSingle();
            EnsureCamera();
            EnsureHolders();
            NetworkManager.EnsureExists();
        }

        private void Start()
        {
            ApplyLocalTestWallet();
            StartCoroutine(BuildPageRoutine());
        }

        private void OnEnable()
        {
            if (CoinsHolder.Instance)
            {
                CoinsHolder.Instance.ChangeEvent.AddListener(OnCoinsChanged);
                CoinsHolder.Instance.LoadEvent.AddListener(OnCoinsChanged);
            }

            TournamentPageLifecycle.OnPageShown(RefreshWallet);

            if (pageBuilt && !ApiConfig.Current.UseLocalSimulation)
                StartCoroutine(SyncWalletRoutine());

            RefreshWallet();
            TournamentJoinFlowGuard.LogState("TournamentPageController.OnEnable");
        }

        private void OnDisable()
        {
            if (CoinsHolder.Instance)
            {
                CoinsHolder.Instance.ChangeEvent.RemoveListener(OnCoinsChanged);
                CoinsHolder.Instance.LoadEvent.RemoveListener(OnCoinsChanged);
            }
        }

        private void OnDestroy()
        {
            if (pageRoot)
                SimpleTween.ForceCancel(pageRoot.gameObject);
        }

        private void EnsureCamera()
        {
            Camera cam = Camera.main;
            if (!cam)
            {
                GameObject camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cam = camGo.GetComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 9.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = TournamentPremiumTheme.EmeraldDark;
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        private void EnsureHolders()
        {
            if (!CoinsHolder.Instance)
            {
                GameObject prefab = Resources.Load<GameObject>(ScriptableHolderResource);
                if (prefab)
                    Instantiate(prefab);
                else
                    Debug.LogError("[Tournament] Missing Resources/" + ScriptableHolderResource);
            }

            ApplyLocalTestWallet();
        }

        private static void ApplyLocalTestWallet()
        {
            if (!ApiConfig.Current.UseLocalSimulation || !CoinsHolder.Instance)
                return;

            if (CoinsHolder.Count < LocalTestWalletCoins)
                CoinsHolder.Instance.SetCount(LocalTestWalletCoins);
        }

        private IEnumerator BuildPageRoutine()
        {
            Sprite pageSprite = TournamentUITheme.PageDesign;
            if (!pageSprite)
            {
                Debug.LogError("Tournament page image missing: Resources/Tournament/turnamant1.png");
                yield break;
            }

            yield return TournamentSpriteFactory.WarmUpCoroutine();

            float pageW = TournamentPngLayout.RefWidth;
            float pageH = TournamentPngLayout.RefHeight;

            Canvas canvas = CreateCanvas();
            pageRoot = TournamentUIFactory.CreateRect(canvas.transform, "TournamentPage");
            TournamentUIFactory.StretchRect(pageRoot);
            yield return null;

            RectTransform scrollRoot = TournamentUIFactory.CreateRect(pageRoot, "MainScroll");
            TournamentUIFactory.StretchRect(scrollRoot);

            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.scrollSensitivity = 28f;

            RectTransform viewport = TournamentUIFactory.CreateRect(scrollRoot, "Viewport");
            TournamentUIFactory.StretchRect(viewport);
            Image maskImg = viewport.gameObject.AddComponent<Image>();
            maskImg.color = new Color(1f, 1f, 1f, 0.01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = viewport;

            scrollContent = TournamentUIFactory.CreateRect(viewport, "Content");
            scrollContent.anchorMin = new Vector2(0.5f, 1f);
            scrollContent.anchorMax = new Vector2(0.5f, 1f);
            scrollContent.pivot = new Vector2(0.5f, 1f);
            scrollContent.sizeDelta = new Vector2(pageW, pageH + TournamentPngLayout.ScrollBottomPadding);
            scroll.content = scrollContent;

            Image pageImage = scrollContent.gameObject.AddComponent<Image>();
            pageImage.sprite = pageSprite;
            pageImage.color = Color.white;
            pageImage.preserveAspect = false;
            pageImage.raycastTarget = false;
            yield return null;

            RectTransform overlay = TournamentUIFactory.CreateRect(scrollContent, "Overlay");
            TournamentUIFactory.StretchRect(overlay);
            overlayRoot = overlay;

            Image scrollCatcher = TournamentUIFactory.CreateImage(overlay, "ScrollCatcher", new Color(1f, 1f, 1f, 0.01f), TournamentSpriteFactory.SoftCircle, true);
            TournamentUIFactory.StretchRect(scrollCatcher.rectTransform);
            scrollCatcher.transform.SetAsFirstSibling();

            RectTransform hitAreas = TournamentUIFactory.CreateRect(overlay, "HitAreas");
            TournamentUIFactory.StretchRect(hitAreas);
            hitAreas.gameObject.AddComponent<TournamentJoinButtonsSelfTest>();

            TournamentUIFactory.CreateInvisibleButton(hitAreas, "BackButton", TournamentPngLayout.Back, OnBackClicked);
            yield return null;

            int cardIndex = 0;
            foreach (TournamentDefinition tournament in TournamentCatalog.All)
            {
                GameObject cardGo = new GameObject("Card_" + tournament.id, typeof(RectTransform), typeof(TournamentCardView));
                cardGo.transform.SetParent(overlay, false);
                TournamentUIFactory.StretchRect(cardGo.GetComponent<RectTransform>());

                TournamentCardView cardView = cardGo.GetComponent<TournamentCardView>();
                cardView.Setup(tournament, OnJoinTournament, 0f, cardIndex, hitAreas);
                cardView.BindJoinButton(hitAreas, TournamentPngLayout.GetJoinRect(cardIndex), OnJoinTournament);
                cardIndex++;
                yield return null;
            }

            int balance = CoinsHolder.Instance ? CoinsHolder.Count : 0;
            TournamentCardOverlays.Build(overlay, balance);
            yield return null;

            walletText = TournamentUIFactory.CreateWalletBalance(overlay);
            walletPulse = walletText.gameObject.AddComponent<TournamentWalletPulse>();

            onlineStatusText = TournamentUIFactory.CreateOverlayText(
                overlay,
                "OnlineStatus",
                new Rect(24f, 118f, 320f, 28f),
                ApiConfig.Current.UseLocalSimulation ? "● Offline Practice" : "● Live Server",
                TournamentPngLayout.OverlayFont(14f),
                FontStyle.Bold,
                ApiConfig.Current.UseLocalSimulation
                    ? TournamentPremiumTheme.TextMuted
                    : new Color(0.45f, 1f, 0.62f),
                TextAnchor.MiddleLeft);

            RefreshWallet();

            hitAreas.SetAsLastSibling();

            RectTransform depositButton = TournamentUIFactory.CreateDepositButton(overlay, () => OpenDepositPanel(null));
            depositButton.SetAsLastSibling();
            yield return null;

            dialog = TournamentDialog.Create(pageRoot);
            waitingRoom = TournamentWaitingRoomPanel.Create(pageRoot);
            yield return null;

            TournamentPageIntro.Play(pageRoot.gameObject);

            TournamentPageResponsive responsive = pageRoot.gameObject.AddComponent<TournamentPageResponsive>();
            responsive.Configure(scrollRoot, scrollContent);

            pageBuilt = true;
            RefreshWallet();
            TournamentJoinFlowGuard.LogState("TournamentPageController.BuildPageRoutine complete");

            if (!ApiConfig.Current.UseLocalSimulation)
                StartCoroutine(SyncOnlineDataRoutine());
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasGo = new GameObject("TournamentCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 50f;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(TournamentPngLayout.RefWidth, TournamentPngLayout.RefHeight);
            scaler.matchWidthOrHeight = Screen.height > Screen.width * 1.85f ? 0f : 0.5f;

            return canvas;
        }

        private void OnJoinTournament(TournamentDefinition tournament)
        {
            try
            {
                if (tournament != null && tournament.id == TournamentJoinDebug.FirstJoinId)
                    Debug.Log("[TournamentJoin] ENTER OnJoinTournament");

                if (!TournamentJoinFlowGuard.CheckCanStartJoin("TournamentPageController.OnJoinTournament"))
                    return;

                TournamentJoinDebug.LogOnJoinTournamentEnter(tournament);

                int balance = CoinsHolder.Instance ? CoinsHolder.Count : 0;
                if (balance < tournament.entryFee)
                {
                    if (TournamentJoinDebug.IsFirstJoin(tournament))
                    {
                        TournamentJoinDebug.LogDialogOpening("OnJoinTournament insufficient coins", "Insufficient Coins");
                    }

                    PromptDepositForJoin(tournament, balance);
                    return;
                }

                if (TournamentJoinDebug.IsFirstJoin(tournament))
                {
                    TournamentJoinDebug.LogDialogOpening("OnJoinTournament confirm join", "Join Tournament?");
                }

                TournamentDefinition currentTournament = tournament;
                int winPrize = TournamentPrizeTable.GetPrize(currentTournament.id, 1);
                dialog.ShowJoinConfirm(
                    currentTournament.displayName,
                    currentTournament.entryFee,
                    winPrize,
                    () => ConfirmJoin(currentTournament),
                    null);

                if (TournamentJoinDebug.IsFirstJoin(tournament))
                    TournamentJoinDebug.Log("OnJoinTournament completed — confirm dialog requested");
            }
            catch (Exception ex)
            {
                TournamentJoinDebug.LogException("OnJoinTournament", ex);
                throw;
            }
        }

        private void ConfirmJoin(TournamentDefinition tournament)
        {
            try
            {
                if (tournament != null && tournament.id == TournamentJoinDebug.FirstJoinId)
                    Debug.Log("[TournamentJoin] ENTER ConfirmJoin");

                if (!TournamentJoinFlowGuard.TryBegin())
                    return;

                TournamentJoinDebug.LogConfirmJoinEnter(tournament);
                TournamentJoinCoordinator.ConfirmJoin(
                    tournament,
                    dialog,
                    waitingRoom,
                    RefreshWallet,
                    t => OnJoinTournament(t),
                    () => TournamentJoinFlowGuard.Reset());
            }
            catch (Exception ex)
            {
                TournamentJoinDebug.LogException("ConfirmJoin", ex);
                throw;
            }
        }

        private IEnumerator SyncOnlineDataRoutine()
        {
            bool catalogOk = false;
            var catalogTask = FetchTournamentCatalogWithRetryAsync();
            while (!catalogTask.IsCompleted)
                yield return null;

            catalogOk = catalogTask.Result;
            SetOnlineStatus(catalogOk);

            yield return EnsureSessionRoutine();

            var leaderboardTask = LeaderboardService.FetchLeaderboardAsync();
            while (!leaderboardTask.IsCompleted)
                yield return null;

            var historyTask = TournamentService.FetchHistoryAsync();
            while (!historyTask.IsCompleted)
                yield return null;

            if (historyTask.Result.Success)
                TournamentHistoryService.ApplyApiHistory(historyTask.Result.Data);

            yield return SyncWalletRoutine();

            if (!catalogOk)
                StartCoroutine(RefreshOnlineDataRoutine());
        }

        private IEnumerator EnsureSessionRoutine()
        {
            if (NetworkManager.Instance.IsAuthenticated)
                yield break;

            var loginTask = GuestLoginWithRetryAsync();
            while (!loginTask.IsCompleted)
                yield return null;

            if (!loginTask.Result.Success)
            {
                Debug.LogWarning(
                    "[TournamentPage] Guest login failed: " + loginTask.Result.ErrorMessage +
                    " (status " + loginTask.Result.StatusCode + "). Join may retry login.");
            }
        }

        private IEnumerator RefreshOnlineDataRoutine()
        {
            var catalogTask = FetchTournamentCatalogWithRetryAsync();
            while (!catalogTask.IsCompleted)
                yield return null;
            SetOnlineStatus(catalogTask.Result);

            yield return EnsureSessionRoutine();
            yield return SyncWalletRoutine();

            if (!catalogTask.Result)
                yield return new WaitForSecondsRealtime(2f);
        }

        private static async System.Threading.Tasks.Task<bool> FetchTournamentCatalogWithRetryAsync()
        {
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var result = await TournamentService.FetchTournamentListAsync();
                if (result.Success)
                    return true;

                Debug.LogWarning(
                    "[TournamentPage] Tournament catalog fetch failed (" + attempt + "/" + maxAttempts + "): " +
                    result.ErrorMessage + " status=" + result.StatusCode);

                if (!result.IsServerUnavailable && result.StatusCode != 0)
                    return false;

                if (attempt < maxAttempts)
                    await System.Threading.Tasks.Task.Delay(attempt * 1000);
            }

            return false;
        }

        private IEnumerator SyncWalletRoutine()
        {
            var walletTask = WalletService.SyncToCoinsHolderAsync();
            while (!walletTask.IsCompleted)
                yield return null;

            RefreshWallet();
        }

        private static async System.Threading.Tasks.Task<ApiResult<TokenResponseDto>> GuestLoginWithRetryAsync()
        {
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var login = await AuthService.GuestLoginAsync();
                if (login.Success)
                    return login;

                if (!login.IsServerUnavailable && login.StatusCode != 0)
                    return login;

                if (attempt < maxAttempts)
                    await System.Threading.Tasks.Task.Delay(attempt * 1000);
            }

            return await AuthService.GuestLoginAsync();
        }

        private void ShowServerUnavailableRetry(Action retry)
        {
            if (!dialog)
                return;

            dialog.Show(
                "Connection Problem",
                "Could not reach the game server.\nPlease check internet and try again.",
                true,
                () => retry?.Invoke(),
                null);
        }

        private void SetOnlineStatus(bool connected)
        {
            if (!onlineStatusText || ApiConfig.Current.UseLocalSimulation)
                return;

            if (connected)
            {
                onlineStatusText.text = "● Live Server";
                onlineStatusText.color = new Color(0.45f, 1f, 0.62f);
            }
            else
            {
                onlineStatusText.text = "○ Reconnecting...";
                onlineStatusText.color = new Color(1f, 0.72f, 0.42f);
            }
        }

        private void OnBackClicked()
        {
            if (SceneLoader.Instance)
                SceneLoader.Instance.LoadScene(backSceneIndex);
            else
                SceneManager.LoadScene(backSceneIndex);
        }

        private void OnCoinsChanged(int _)
        {
            RefreshWallet();
        }

        private void RefreshWallet()
        {
            if (!walletText) return;
            if (!CoinsHolder.Instance)
            {
                walletText.text = string.Empty;
                return;
            }

            int balance = CoinsHolder.Count;
            walletText.text = balance.ToString("N0");
            walletPulse?.NotifyBalance(balance);
            TournamentCardOverlays.RefreshAffordability(overlayRoot, balance);
        }

        private void OpenDepositPanel(Action onComplete)
        {
            if (!dialog)
                return;

            dialog.ShowDepositMenu(() =>
            {
                RefreshWallet();
                onComplete?.Invoke();
            });
        }

        private void PromptDepositForJoin(TournamentDefinition tournament, int balance)
        {
            dialog.ShowInsufficientCoins(
                tournament.entryFee,
                balance,
                () => OpenDepositPanel(() => OnJoinTournament(tournament)),
                null);
        }

    }
}
