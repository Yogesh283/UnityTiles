using System;
using Mkey;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Standard wooden Message popup (same as Connection Problem / join dialogs).
    /// </summary>
    public static class TournamentMessagePopup
    {
        private static bool visible;

        public static bool IsVisible => visible;

        public static void Show(string title, string message, Action onOk = null, string okLabel = "Ok")
        {
            WarningMessController prefab = Resources.Load<WarningMessController>("PopUps/Message");
            GuiController gui = EnsureGuiController();
            if (!gui || !prefab)
            {
                Debug.LogError("[Tournament] Message popup missing: Resources/PopUps/Message");
                onOk?.Invoke();
                return;
            }

            visible = true;
            WarningMessController popup = gui.ShowMessageWithYesNoCloseButton(
                prefab,
                title ?? string.Empty,
                message ?? string.Empty,
                () =>
                {
                    visible = false;
                    onOk?.Invoke();
                },
                null,
                null);

            SetButtonLabel(popup?.yesButton, okLabel);
            if (popup?.cancelButton)
                popup.cancelButton.gameObject.SetActive(false);
            if (popup?.noButton)
                popup.noButton.gameObject.SetActive(false);
        }

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

            GuiController existing = UnityEngine.Object.FindFirstObjectByType<GuiController>();
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
