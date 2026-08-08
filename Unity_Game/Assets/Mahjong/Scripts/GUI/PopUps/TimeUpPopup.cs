using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Clean Time's Up dialog: Home + Restart.
    /// </summary>
    public class TimeUpPopup : MonoBehaviour
    {
        private static TimeUpPopup instance;
        private Action onHome;
        private Action onRestart;
        private bool closed;

        public static void Show(Action onHome, Action onRestart)
        {
            if (instance)
            {
                instance.CloseImmediate();
            }

            Canvas canvas = ResolveCanvas();
            if (!canvas)
            {
                onRestart?.Invoke();
                return;
            }

            GameObject root = new GameObject("TimeUpPopup", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            instance = root.AddComponent<TimeUpPopup>();
            instance.onHome = onHome;
            instance.onRestart = onRestart;
            instance.Build(root.GetComponent<RectTransform>());
            root.transform.SetAsLastSibling();
        }

        private void Build(RectTransform root)
        {
            StretchFull(root);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // Dim
            Image dim = CreateImage(root, "Dim", new Color(0.02f, 0.03f, 0.06f, 0.72f), true);
            StretchFull(dim.rectTransform);

            // Card
            RectTransform card = CreateRect(root, "Card");
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(560f, 420f);
            card.anchoredPosition = Vector2.zero;

            Image cardBg = CreateImage(card, "Bg", new Color(0.08f, 0.09f, 0.12f, 0.98f), false);
            StretchFull(cardBg.rectTransform);
            Outline rim = cardBg.gameObject.AddComponent<Outline>();
            rim.effectColor = new Color(0.85f, 0.7f, 0.28f, 0.85f);
            rim.effectDistance = new Vector2(2.5f, -2.5f);

            // Inner soft panel
            Image inner = CreateImage(card, "Inner", new Color(0.12f, 0.13f, 0.17f, 1f), false);
            Stretch(inner.rectTransform, 0.06f, 0.08f, 0.94f, 0.92f);

            Text title = CreateText(card, "Title", "Time's Up!", 42, FontStyle.Bold,
                new Color(1f, 0.86f, 0.4f, 1f), TextAnchor.MiddleCenter, font);
            Stretch(title.rectTransform, 0.08f, 0.68f, 0.92f, 0.88f);

            Text message = CreateText(card, "Message",
                "5 minutes are over.\nReady to try again?",
                24, FontStyle.Normal, new Color(0.92f, 0.92f, 0.95f, 1f), TextAnchor.MiddleCenter, font);
            Stretch(message.rectTransform, 0.1f, 0.42f, 0.9f, 0.66f);

            // Buttons row
            RectTransform row = CreateRect(card, "Buttons");
            Stretch(row, 0.08f, 0.12f, 0.92f, 0.36f);

            Button homeBtn = CreatePillButton(row, "HomeBtn", "Home",
                new Color(0.28f, 0.32f, 0.4f, 1f), font, 0.02f, 0.48f);
            homeBtn.onClick.AddListener(Home_Click);

            Button restartBtn = CreatePillButton(row, "RestartBtn", "Restart",
                new Color(0.18f, 0.55f, 0.32f, 1f), font, 0.52f, 0.98f);
            restartBtn.onClick.AddListener(Restart_Click);

            // Soft pop-in
            card.localScale = Vector3.one * 0.86f;
            SimpleTween.Value(gameObject, 0.86f, 1f, 0.22f)
                .SetEase(EaseAnim.EaseOutCubic)
                .SetOnUpdate((float v) =>
                {
                    if (card) card.localScale = Vector3.one * v;
                });
        }

        private void Home_Click()
        {
            if (closed) return;
            closed = true;
            Action cb = onHome;
            CloseImmediate();
            cb?.Invoke();
        }

        private void Restart_Click()
        {
            if (closed) return;
            closed = true;
            Action cb = onRestart;
            CloseImmediate();
            cb?.Invoke();
        }

        private void CloseImmediate()
        {
            if (instance == this) instance = null;
            if (gameObject) Destroy(gameObject);
        }

        private void OnDestroy()
        {
            SimpleTween.Cancel(gameObject, false);
            if (instance == this) instance = null;
        }

        #region ui helpers
        private static Button CreatePillButton(
            RectTransform parent, string name, string label, Color color, Font font,
            float x0, float x1)
        {
            Image img = CreateImage(parent, name, color, true);
            Stretch(img.rectTransform, x0, 0.15f, x1, 0.85f);

            Outline o = img.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.35f);
            o.effectDistance = new Vector2(1.5f, -1.5f);

            Button btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = cb;

            Text text = CreateText(img.rectTransform, "Label", label, 28, FontStyle.Bold,
                Color.white, TextAnchor.MiddleCenter, font);
            StretchFull(text.rectTransform);
            return btn;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image CreateImage(Transform parent, string name, Color color, bool raycast)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        private static Text CreateText(
            Transform parent, string name, string content, int size, FontStyle style,
            Color color, TextAnchor anchor, Font font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = (font != null && font.dynamic) ? style : FontStyle.Normal;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Canvas ResolveCanvas()
        {
            GameObject canvasOver = GameObject.Find("CanvasOver(for popups)");
            if (canvasOver && canvasOver.TryGetComponent(out Canvas overCanvas))
                return overCanvas;

            if (GuiController.Instance)
            {
                Canvas c = GuiController.Instance.GetComponentInParent<Canvas>();
                if (c) return c;
            }

            GameObject canvasMain = GameObject.Find("CanvasMain");
            if (canvasMain && canvasMain.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            return FindFirstObjectByType<Canvas>();
        }
        #endregion
    }
}
