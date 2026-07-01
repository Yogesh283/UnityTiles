using System;
using System.Collections;
using Mkey;
using Mkey.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Routes tournament messages through the same GuiController popup used on map/game scenes.
    /// </summary>
    public class TournamentDialog : MonoBehaviour
    {
        private WarningMessController messagePrefab;

        public static TournamentDialog Create(Transform parent)
        {
            GameObject go = new GameObject("TournamentDialog");
            go.transform.SetParent(parent, false);
            TournamentDialog dialog = go.AddComponent<TournamentDialog>();
            dialog.Initialize();
            return dialog;
        }

        private void Initialize()
        {
            messagePrefab = Resources.Load<WarningMessController>("PopUps/Message");
            if (!messagePrefab)
                Debug.LogError("Tournament popup missing: Resources/PopUps/Message.prefab");
            EnsureGuiController();
        }

        public void ShowInsufficientCoins(int required, int balance, Action onDeposit, Action onCancel = null)
        {
            int need = Mathf.Max(0, required - balance);
            string message =
                $"You need {required:N0} coins to join.\n" +
                $"Your balance: {balance:N0}\n" +
                $"Short by: {need:N0} coins";

            GuiController gui = EnsureGuiController();
            if (!gui || !messagePrefab)
            {
                TournamentJoinDebug.LogDialogFailed("TournamentDialog.ShowInsufficientCoins", !gui ? "GuiController is null" : "Message prefab is null");
                return;
            }

            WarningMessController popup = gui.ShowMessageWithYesNoCloseButton(
                messagePrefab,
                "Insufficient Coins",
                message,
                onDeposit,
                onCancel,
                null);

            SetButtonLabel(popup?.yesButton, "Deposit");
            SetButtonLabel(popup?.cancelButton, "Cancel");
        }

        public void ShowDepositMenu(Action onComplete = null)
        {
            if (ApiConfig.Current.UseLocalSimulation)
            {
                Show(
                    "Deposit Coins",
                    "Purchases are disabled in development mode.\nUse Admin Panel to add coins for testing.",
                    false,
                    null,
                    null);
                return;
            }

            StartCoroutine(ShowDepositMenuRoutine(onComplete));
        }

        private IEnumerator ShowDepositMenuRoutine(Action onComplete)
        {
            if (!PaymentService.BillingActive)
            {
                var statusTask = PaymentService.FetchBillingStatusAsync();
                while (!statusTask.IsCompleted)
                    yield return null;
            }

            if (!PaymentService.BillingActive)
            {
                Show(
                    "Deposit Coins",
                    "Play Store payments are not active yet.\n" + PaymentService.BillingStatusMessage,
                    false,
                    null,
                    null);
                yield break;
            }

            TournamentDepositService.EnsurePurchaser();
            ShowDepositPack(0, onComplete);
        }

        private void ShowDepositPack(int packIndex, Action onComplete)
        {
            CoinPack pack = TournamentDepositService.GetPack(packIndex);
            string price = TournamentDepositService.GetPriceLabel(pack.Id, pack.PriceInr);
            string message =
                $"Add coins to join tournaments.\n\n" +
                $"{pack.Coins:N0} Coins\n" +
                $"Price: {price}\n\n" +
                $"Pack {packIndex + 1} of {TournamentDepositService.PackCount}";

            GuiController gui = EnsureGuiController();
            if (!gui || !messagePrefab)
                return;

            int nextIndex = (packIndex + 1) % TournamentDepositService.PackCount;
            string productId = pack.Id;

            WarningMessController popup = gui.ShowMessageWithYesNoCloseButton(
                messagePrefab,
                "Deposit Coins",
                message,
                () => BuyPack(productId, onComplete),
                null,
                () => ShowDepositPack(nextIndex, onComplete));

            SetButtonLabel(popup?.yesButton, "Buy");
            SetButtonLabel(popup?.noButton, "Next");
            SetButtonLabel(popup?.cancelButton, "Close");
        }

        private void BuyPack(string productId, Action onComplete)
        {
            TournamentDepositService.EnsurePurchaser();
            if (!Purchaser.Instance)
            {
                Show("Deposit", "Store is not available right now.", false, null, null);
                return;
            }

            Purchaser.Instance.GoodPurchaseEvent -= OnPurchaseSuccess;
            Purchaser.Instance.FailedPurchaseEvent -= OnPurchaseFailed;
            Purchaser.Instance.GoodPurchaseEvent += OnPurchaseSuccess;
            Purchaser.Instance.FailedPurchaseEvent += OnPurchaseFailed;
            _depositCompleteCallback = onComplete;
            Purchaser.Instance.BuyProductID(productId);
        }

        private Action _depositCompleteCallback;

        private void OnPurchaseSuccess(string productId, string productName)
        {
            if (Purchaser.Instance)
            {
                Purchaser.Instance.GoodPurchaseEvent -= OnPurchaseSuccess;
                Purchaser.Instance.FailedPurchaseEvent -= OnPurchaseFailed;
            }

            StartCoroutine(SyncAfterPurchase());
        }

        private void OnPurchaseFailed(string productId, string productName)
        {
            if (Purchaser.Instance)
            {
                Purchaser.Instance.GoodPurchaseEvent -= OnPurchaseSuccess;
                Purchaser.Instance.FailedPurchaseEvent -= OnPurchaseFailed;
            }

            _depositCompleteCallback = null;
        }

        private IEnumerator SyncAfterPurchase()
        {
            if (!ApiConfig.Current.UseLocalSimulation)
            {
                var task = WalletService.SyncToCoinsHolderAsync();
                while (!task.IsCompleted)
                    yield return null;
            }

            Action callback = _depositCompleteCallback;
            _depositCompleteCallback = null;
            callback?.Invoke();
        }

        public void ShowJoinConfirm(
            string tournamentName,
            int entryFee,
            int winPrize,
            Action confirm,
            Action cancel)
        {
            string message =
                $"Entry Fee\n{entryFee:N0} Coins\n\n" +
                $"Winning Prize\n{winPrize:N0} Coins";

            GuiController gui = EnsureGuiController();
            if (!gui || !messagePrefab)
            {
                TournamentJoinDebug.LogDialogFailed("TournamentDialog.ShowJoinConfirm", !gui ? "GuiController is null" : "Message prefab is null");
                return;
            }

            WarningMessController popup = gui.ShowMessageWithYesNoCloseButton(
                messagePrefab,
                tournamentName,
                message,
                confirm,
                cancel,
                null);

            SetButtonLabel(popup?.yesButton, "OK");
            SetButtonLabel(popup?.cancelButton, "Cancel");
            TournamentJoinDebug.Log("TournamentDialog opened join confirm (OK/Cancel)");
        }

        public void Show(string title, string message, bool showCancel, Action confirm, Action cancel)
        {
            TournamentJoinDebug.Log($"TournamentDialog.Show called — title=\"{title}\", showCancel={showCancel}");

            GuiController gui = EnsureGuiController();
            if (!gui || !messagePrefab)
            {
                TournamentJoinDebug.LogDialogFailed("TournamentDialog.Show", !gui ? "GuiController is null" : "Message prefab is null");
                return;
            }

            if (showCancel)
            {
                WarningMessController popup = gui.ShowMessageWithYesNoCloseButton(messagePrefab, title, message, confirm, cancel, null);
                SetButtonLabel(popup?.yesButton, "OK");
                SetButtonLabel(popup?.cancelButton, "Cancel");
                TournamentJoinDebug.Log("TournamentDialog opened via GuiController (Yes/Cancel)");
                return;
            }

            if (confirm != null)
            {
                WarningMessController popup = gui.ShowMessageWithYesNoCloseButton(messagePrefab, title, message, confirm, null, null);
                SetButtonLabel(popup?.yesButton, "Ok");
                TournamentJoinDebug.Log("TournamentDialog opened via GuiController (Yes only)");
            }
            else
            {
                gui.ShowMessageWithYesNoCloseButton(messagePrefab, title, message, () => { }, null, null);
                TournamentJoinDebug.Log("TournamentDialog opened via GuiController (OK/Close)");
            }
        }

        public void Hide() { }

        private static void SetButtonLabel(Button button, string label)
        {
            if (!button)
                return;

            Text text = button.GetComponentInChildren<Text>();
            if (text)
                text.text = label;
        }

        private static GuiController EnsureGuiController()
        {
            if (GuiController.Instance)
                return GuiController.Instance;

            GuiController existing = FindFirstObjectByType<GuiController>();
            if (existing)
                return existing;

            GameObject go = new GameObject(
                "GuiController",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(GuiController));

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.4f;

            return go.GetComponent<GuiController>();
        }
    }
}
