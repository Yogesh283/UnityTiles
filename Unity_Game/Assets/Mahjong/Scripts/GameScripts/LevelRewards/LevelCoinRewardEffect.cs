using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Short floating coin burst shown when a level completion reward is granted.
    /// </summary>
    public static class LevelCoinRewardEffect
    {
        public static void Play(int amount)
        {
            Canvas canvas = ResolveCanvas();
            if (!canvas) return;

            GameObject root = new GameObject("LevelCoinRewardEffect");
            root.transform.SetParent(canvas.transform, false);

            RectTransform rt = root.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.45f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420f, 120f);
            rt.anchoredPosition = Vector2.zero;

            Text text = root.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = $"+{amount} 🪙";
            text.fontSize = 52;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.86f, 0.2f, 1f);
            text.raycastTarget = false;

            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.18f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            rt.localScale = Vector3.zero;
            SimpleTween.Value(root, 0f, 1f, 0.28f)
                .SetEase(EaseAnim.EaseOutBack)
                .SetOnUpdate(t => { if (rt) rt.localScale = Vector3.one * t; });

            SimpleTween.Value(root, 0f, 160f, 1.1f)
                .SetDelay(0.15f)
                .SetEase(EaseAnim.EaseOutCubic)
                .SetOnUpdate(y => { if (rt) rt.anchoredPosition = new Vector2(0f, y); });

            SimpleTween.Value(root, 1f, 0f, 0.35f)
                .SetDelay(0.95f)
                .SetOnUpdate(a =>
                {
                    if (!text) return;
                    Color c = text.color;
                    c.a = a;
                    text.color = c;
                })
                .AddCompleteCallBack(() =>
                {
                    if (root) Object.Destroy(root);
                });
        }

        private static Canvas ResolveCanvas()
        {
            if (GuiController.Instance)
                return GuiController.Instance.GetComponent<Canvas>();

            GameObject canvasMain = GameObject.Find("CanvasMain");
            if (canvasMain && canvasMain.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            GameObject canvasOver = GameObject.Find("CanvasOver(for popups)");
            if (canvasOver && canvasOver.TryGetComponent(out Canvas overCanvas))
                return overCanvas;

            return Object.FindFirstObjectByType<Canvas>();
        }
    }
}
