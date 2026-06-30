using UnityEngine;
using UnityEngine.UI;
/*
 *changes
 * 02102019 Fix
 *      -if (yesButton) yesButton.gameObject.SetActive(yesButtonActive);
        -if (cancelButton) cancelButton.gameObject.SetActive(cancelButtonActive);
        -if (noButton) noButton.gameObject.SetActive(noButtonActive);
 */
namespace Mkey
{
    public enum MessageAnswer { None , Yes, Cancel, No }
    public class WarningMessController : PopUpsController
    {
        public Text caption;
        public Text message;
        public Button yesButton;
        public Button noButton;
        public Button cancelButton;

        public MessageAnswer Answer
        {
            get; private set;
        }

        public void Cancel_Click()
        {
            Answer = MessageAnswer.Cancel;
            CloseWindow();
        }

        public void Yes_Click()
        {
            Answer = MessageAnswer.Yes;
            CloseWindow();
        }

        public void No_Click()
        {
            Answer = MessageAnswer.No;
            CloseWindow();
        }

        public string Caption
        {
            get { if (caption) return caption.text; else return string.Empty; }
            set
            {
                if (caption)
                    caption.text = value;
                ConfigureMessageTextLayout();
            }
        }

        public string Message
        {
            get { if (message) return message.text; else return string.Empty; }
            set
            {
                if (message)
                    message.text = value;
                ConfigureMessageTextLayout();
            }
        }

        internal void SetMessage(string caption, string message, bool yesButtonActive, bool cancelButtonActive, bool noButtonActive)
        {
            if (this.caption)
                this.caption.text = caption;
            if (this.message)
                this.message.text = message;

            ConfigureMessageTextLayout();
            ConfigureButtonsInsidePanel(yesButtonActive || cancelButtonActive || noButtonActive);

            if (yesButton) yesButton.gameObject.SetActive(yesButtonActive);
            if (cancelButton) cancelButton.gameObject.SetActive(cancelButtonActive);
            if (noButton) noButton.gameObject.SetActive(noButtonActive);
        }

        private void ConfigureMessageTextLayout()
        {
            bool hasCaption = caption && !string.IsNullOrWhiteSpace(caption.text);
            bool hasMessage = message && !string.IsNullOrWhiteSpace(message.text);

            if (hasCaption && hasMessage)
            {
                ApplyTextInsidePanel(caption, 0.68f, 0.90f, 20, 28, TextAnchor.UpperCenter);
                ApplyTextInsidePanel(message, 0.30f, 0.66f, 16, 24, TextAnchor.UpperCenter);
                caption.gameObject.SetActive(true);
                message.gameObject.SetActive(true);
                return;
            }

            if (hasCaption)
            {
                ApplyTextInsidePanel(caption, 0.30f, 0.86f, 18, 28, TextAnchor.MiddleCenter);
                if (message) message.gameObject.SetActive(false);
                caption.gameObject.SetActive(true);
                return;
            }

            if (hasMessage)
            {
                ApplyTextInsidePanel(message, 0.30f, 0.86f, 16, 26, TextAnchor.MiddleCenter);
                if (caption) caption.gameObject.SetActive(false);
                message.gameObject.SetActive(true);
                return;
            }

            if (caption) caption.gameObject.SetActive(false);
            if (message) message.gameObject.SetActive(false);
        }

        private void ConfigureButtonsInsidePanel(bool visible)
        {
            RectTransform buttonsRoot = FindButtonsRoot();
            if (!buttonsRoot)
                return;

            buttonsRoot.gameObject.SetActive(visible);
            if (!visible)
                return;

            buttonsRoot.anchorMin = new Vector2(0.14f, 0.08f);
            buttonsRoot.anchorMax = new Vector2(0.86f, 0.24f);
            buttonsRoot.offsetMin = Vector2.zero;
            buttonsRoot.offsetMax = Vector2.zero;
            buttonsRoot.pivot = new Vector2(0.5f, 0.5f);
            buttonsRoot.anchoredPosition = Vector2.zero;

            if (yesButton)
                FitButtonInsideRow(yesButton.GetComponent<RectTransform>());
            if (noButton)
                FitButtonInsideRow(noButton.GetComponent<RectTransform>());
            if (cancelButton)
                FitButtonInsideRow(cancelButton.GetComponent<RectTransform>());
        }

        private RectTransform FindButtonsRoot()
        {
            if (yesButton)
                return yesButton.transform.parent as RectTransform;
            if (noButton)
                return noButton.transform.parent as RectTransform;
            if (cancelButton)
                return cancelButton.transform.parent as RectTransform;
            return null;
        }

        private static void FitButtonInsideRow(RectTransform button)
        {
            if (!button)
                return;

            button.anchorMin = new Vector2(0.5f, 0.5f);
            button.anchorMax = new Vector2(0.5f, 0.5f);
            button.pivot = new Vector2(0.5f, 0.5f);
            button.anchoredPosition = Vector2.zero;
            button.sizeDelta = new Vector2(170f, 72f);
        }

        private static void ApplyTextInsidePanel(
            Text text,
            float minY,
            float maxY,
            int minSize,
            int maxSize,
            TextAnchor alignment)
        {
            if (!text)
                return;

            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;

            RectTransform rt = text.rectTransform;
            rt.anchorMin = new Vector2(0.12f, minY);
            rt.anchorMax = new Vector2(0.88f, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
