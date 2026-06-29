using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Standard app-wide message popup (Resources/PopUps/Message) used on map, game, and tournament screens.
    /// </summary>
    public static class AppMessageDialog
    {
        private static WarningMessController messagePrefab;

        public static void Show(string title, string message, Action onOk = null)
        {
            GuiController gui = EnsureGuiController();
            WarningMessController prefab = GetMessagePrefab();
            if (!gui || !prefab)
            {
                Debug.LogWarning("AppMessageDialog: popup unavailable.");
                return;
            }

            WarningMessController popup = gui.ShowMessageWithYesNoCloseButton(
                prefab,
                title,
                message,
                onOk ?? (() => { }),
                null,
                null);

            SetButtonLabel(popup?.yesButton, "OK");
        }

        private static WarningMessController GetMessagePrefab()
        {
            if (messagePrefab)
                return messagePrefab;

            messagePrefab = Resources.Load<WarningMessController>("PopUps/Message");
            if (!messagePrefab)
                Debug.LogError("AppMessageDialog: Resources/PopUps/Message.prefab not found.");

            return messagePrefab;
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

        private static void SetButtonLabel(Button button, string label)
        {
            if (!button)
                return;

            Text text = button.GetComponentInChildren<Text>();
            if (text)
                text.text = label;
        }
    }
}
