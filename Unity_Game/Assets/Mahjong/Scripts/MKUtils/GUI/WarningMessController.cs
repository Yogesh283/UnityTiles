using UnityEngine;
using UnityEngine.UI;
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

            if (yesButton) yesButton.gameObject.SetActive(yesButtonActive);
            if (cancelButton) cancelButton.gameObject.SetActive(cancelButtonActive);
            if (noButton) noButton.gameObject.SetActive(noButtonActive);

            RefreshButtonLayout();
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

            // Keep buttons inside the panel art — above the bottom scroll decoration.
            buttonsRoot.anchorMin = new Vector2(0.10f, 0.10f);
            buttonsRoot.anchorMax = new Vector2(0.90f, 0.30f);
            buttonsRoot.offsetMin = Vector2.zero;
            buttonsRoot.offsetMax = Vector2.zero;
            buttonsRoot.pivot = new Vector2(0.5f, 0.5f);
            buttonsRoot.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup layout = buttonsRoot.GetComponent<HorizontalLayoutGroup>();
            if (layout)
            {
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 14f;
                layout.padding = new RectOffset(6, 6, 6, 12);
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
            }

            if (yesButton && IsButtonInRow(yesButton, buttonsRoot))
                LayoutButtonInRow(yesButton);
            if (noButton && IsButtonInRow(noButton, buttonsRoot))
                LayoutButtonInRow(noButton);
            if (cancelButton && IsButtonInRow(cancelButton, buttonsRoot))
                LayoutButtonInRow(cancelButton);

            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsRoot);

            if (yesButton && yesButton.gameObject.activeSelf)
                FitButtonLabel(yesButton);
            if (noButton && noButton.gameObject.activeSelf)
                FitButtonLabel(noButton);
            if (cancelButton && cancelButton.gameObject.activeSelf)
                FitButtonLabel(cancelButton);
        }

        internal void RefreshButtonLayout()
        {
            bool visible =
                (yesButton && yesButton.gameObject.activeSelf) ||
                (noButton && noButton.gameObject.activeSelf) ||
                (cancelButton && cancelButton.gameObject.activeSelf);
            ConfigureButtonsInsidePanel(visible);
        }

        internal static void RefitButtonLabel(Button button)
        {
            if (!button)
                return;

            FitButtonLabel(button);
        }

        private static bool IsButtonInRow(Button button, RectTransform buttonsRoot) =>
            button && button.transform.parent == buttonsRoot;

        private static void LayoutButtonInRow(Button button)
        {
            RectTransform rt = button.GetComponent<RectTransform>();
            if (!rt)
                return;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(320f, 80f);

            LayoutElement layoutElement = button.GetComponent<LayoutElement>();
            if (!layoutElement)
                layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = 280f;
            layoutElement.preferredWidth = 320f;
            layoutElement.minHeight = 68f;
            layoutElement.preferredHeight = 80f;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            ApplyPopupButtonStyle(button);
            FitButtonLabel(button);
        }

        private static void ApplyPopupButtonStyle(Button button)
        {
            Image image = button.targetGraphic as Image;
            if (!image)
                image = button.GetComponent<Image>();
            if (!image)
                return;

            Sprite normal = ResolveGreenButtonSprite();
            if (normal)
                image.sprite = normal;

            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.preserveAspect = false;
            image.enabled = true;

            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.55f);
            button.colors = colors;
        }

        private static Sprite ResolveGreenButtonSprite()
        {
            PopupButtonThemeData theme = Resources.Load<PopupButtonThemeData>("PopupTheme/PopupButtonThemeData");
            if (theme)
            {
                if (theme.greenSmallNormal)
                    return theme.greenSmallNormal;
                if (theme.greenNormal)
                    return theme.greenNormal;
            }

            return null;
        }

        private static readonly Color PopupButtonLabelColor = new Color(1f, 1f, 1f, 1f);

        private static void FitButtonLabel(Button button)
        {
            Text label = button.GetComponentInChildren<Text>(true);
            if (!label)
                return;

            label.gameObject.SetActive(true);
            label.enabled = true;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = false;
            // Bitmap fonts (e.g. Nunito-ExtraBold_0) reject size/style overrides.
            if (label.font != null && label.font.dynamic)
            {
                label.fontSize = 26;
                label.fontStyle = FontStyle.Bold;
            }
            label.supportRichText = false;
            label.raycastTarget = false;
            label.color = PopupButtonLabelColor;

            RectTransform textRt = label.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(6f, 4f);
            textRt.offsetMax = new Vector2(-6f, -4f);
            textRt.anchoredPosition = Vector2.zero;
            textRt.localScale = Vector3.one;

            label.transform.SetAsLastSibling();
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
            if (text.font != null && text.font.dynamic)
            {
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = minSize;
                text.resizeTextMaxSize = maxSize;
            }
            else
            {
                text.resizeTextForBestFit = false;
                text.fontStyle = FontStyle.Normal;
            }

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
