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

        [SerializeField] private GameObject scriptableHolderPrefab;
        [SerializeField] private int backSceneIndex = MapSceneIndex;

        private Text walletText;
        private RectTransform pageRoot;
        private RectTransform scrollContent;
        private TournamentDialog dialog;
        private TournamentWaitingRoomPanel waitingRoom;
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
            BuildPage();
            pageBuilt = true;
            RefreshWallet();

            if (!ApiConfig.Current.UseLocalSimulation)
                StartCoroutine(SyncOnlineDataRoutine());
        }

        private void OnEnable()
        {
            if (CoinsHolder.Instance)
            {
                CoinsHolder.Instance.ChangeEvent.AddListener(OnCoinsChanged);
                CoinsHolder.Instance.LoadEvent.AddListener(OnCoinsChanged);
            }

            if (pageBuilt && !ApiConfig.Current.UseLocalSimulation)
                StartCoroutine(SyncWalletRoutine());

            RefreshWallet();
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
                if (scriptableHolderPrefab)
                    Instantiate(scriptableHolderPrefab);
                else
                {
                    GameObject prefab = Resources.Load<GameObject>("Tournament/ScriptableHolder");
                    if (prefab) Instantiate(prefab);
                }
            }

            if (CoinsHolder.Instance)
                _ = CoinsHolder.Count;
        }

        private void BuildPage()
        {
            Sprite pageSprite = TournamentUITheme.PageDesign;
            if (!pageSprite)
            {
                Debug.LogError("Tournament page image missing: Resources/Tournament/turnamant1.png");
                return;
            }

            float pageW = TournamentPngLayout.RefWidth;
            float pageH = TournamentPngLayout.RefHeight;

            Canvas canvas = CreateCanvas();
            pageRoot = TournamentUIFactory.CreateRect(canvas.transform, "TournamentPage");
            TournamentUIFactory.StretchRect(pageRoot);

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

            RectTransform overlay = TournamentUIFactory.CreateRect(scrollContent, "Overlay");
            TournamentUIFactory.StretchRect(overlay);

            Image scrollCatcher = TournamentUIFactory.CreateImage(overlay, "ScrollCatcher", new Color(1f, 1f, 1f, 0.01f), TournamentSpriteFactory.SoftCircle, true);
            TournamentUIFactory.StretchRect(scrollCatcher.rectTransform);
            scrollCatcher.transform.SetAsFirstSibling();

            RectTransform hitAreas = TournamentUIFactory.CreateRect(overlay, "HitAreas");
            TournamentUIFactory.StretchRect(hitAreas);
            hitAreas.gameObject.AddComponent<TournamentJoinButtonsSelfTest>();

            TournamentUIFactory.CreateInvisibleButton(hitAreas, "BackButton", TournamentPngLayout.Back, OnBackClicked);

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
            }

            walletText = TournamentUIFactory.CreateWalletBalance(overlay);
            RefreshWallet();

            hitAreas.SetAsLastSibling();

            RectTransform depositButton = TournamentUIFactory.CreateDepositButton(overlay, () => OpenDepositPanel(null));
            depositButton.SetAsLastSibling();

            dialog = TournamentDialog.Create(pageRoot);
            waitingRoom = TournamentWaitingRoomPanel.Create(pageRoot);

            TournamentPageResponsive responsive = pageRoot.gameObject.AddComponent<TournamentPageResponsive>();
            responsive.Configure(scrollRoot, scrollContent);
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
                dialog.Show(
                    string.Empty,
                    "Join Tournament?\n\n" +
                    $"Join {currentTournament.displayName}?\n\n" +
                    $"Entry Fee: {currentTournament.entryFee:N0} Coins\n" +
                    $"Prize Pool: {currentTournament.prizePool:N0} Coins",
                    false,
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
                TournamentJoinDebug.LogConfirmJoinEnter(tournament);
                TournamentJoinCoordinator.ConfirmJoin(
                    tournament,
                    dialog,
                    waitingRoom,
                    RefreshWallet,
                    t => OnJoinTournament(t));
            }
            catch (Exception ex)
            {
                TournamentJoinDebug.LogException("ConfirmJoin", ex);
                throw;
            }
        }

        private IEnumerator SyncOnlineDataRoutine()
        {
            bool dataReady = true;

            if (!NetworkManager.Instance.IsAuthenticated)
            {
                var loginTask = GuestLoginWithRetryAsync();
                while (!loginTask.IsCompleted)
                    yield return null;

                if (!loginTask.Result.Success)
                    dataReady = false;
            }
            else
            {
                var sessionTask = NetworkManager.Instance.GetAsync<UserProfileDto>("auth/me");
                while (!sessionTask.IsCompleted)
                    yield return null;

                if (!sessionTask.Result.Success)
                {
                    AuthService.Logout();
                    var reloginTask = GuestLoginWithRetryAsync();
                    while (!reloginTask.IsCompleted)
                        yield return null;

                    if (!reloginTask.Result.Success)
                        dataReady = false;
                }
            }

            var catalogTask = TournamentService.FetchTournamentListAsync();
            while (!catalogTask.IsCompleted)
                yield return null;

            if (!catalogTask.Result.Success)
                dataReady = false;

            var leaderboardTask = LeaderboardService.FetchLeaderboardAsync();
            while (!leaderboardTask.IsCompleted)
                yield return null;

            var historyTask = TournamentService.FetchHistoryAsync();
            while (!historyTask.IsCompleted)
                yield return null;

            if (historyTask.Result.Success)
                TournamentHistoryService.ApplyApiHistory(historyTask.Result.Data);

            yield return SyncWalletRoutine();

            if (!dataReady)
                ShowServerUnavailableRetry(() => StartCoroutine(RefreshOnlineDataRoutine()));
        }

        private IEnumerator RefreshOnlineDataRoutine()
        {
            var catalogTask = TournamentService.FetchTournamentListAsync();
            while (!catalogTask.IsCompleted)
                yield return null;

            yield return SyncWalletRoutine();

            if (!catalogTask.Result.Success)
                ShowServerUnavailableRetry(() => StartCoroutine(RefreshOnlineDataRoutine()));
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
            var login = await AuthService.GuestLoginAsync();
            if (login.Success || !login.IsServerUnavailable)
                return login;

            await System.Threading.Tasks.Task.Delay(750);
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

            walletText.text = CoinsHolder.Count.ToString("N0");
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
