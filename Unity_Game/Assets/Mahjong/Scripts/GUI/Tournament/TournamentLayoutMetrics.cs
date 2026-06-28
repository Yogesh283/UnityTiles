using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Scales tournament UI from PNG reference (852×1846) and applies safe-area insets.
    /// </summary>
    public static class TournamentLayoutMetrics
    {
        public const float RefWidth = 852f;
        public const float RefHeight = 1846f;

        public static float Scale { get; private set; } = 1f;
        public static float WidthScale { get; private set; } = 1f;
        public static float HeightScale { get; private set; } = 1f;
        public static bool Compact { get; private set; }

        public static Rect SafeAreaPx { get; private set; }
        public static Vector4 SafeInsetRef { get; private set; }

        public static void Refresh()
        {
            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            WidthScale = w / RefWidth;
            HeightScale = h / RefHeight;
            Scale = Mathf.Lerp(WidthScale, HeightScale, 0.4f);
            Scale = Mathf.Clamp(Scale, 0.68f, 1.12f);
            Compact = Scale < 0.82f || w / h > 0.52f;

            SafeAreaPx = Screen.safeArea;
            float top = (h - (SafeAreaPx.y + SafeAreaPx.height)) * (RefHeight / h);
            float bottom = SafeAreaPx.y * (RefHeight / h);
            float left = SafeAreaPx.x * (RefWidth / w);
            float right = (w - (SafeAreaPx.x + SafeAreaPx.width)) * (RefWidth / w);
            SafeInsetRef = new Vector4(left, bottom, right, top);
        }

        public static float S(float value) => value * Scale;
        public static int Font(float value) => Mathf.Max(14, Mathf.RoundToInt(value * Scale));
    }

    /// <summary>
    /// Hit areas aligned to turnamant1.png (852×1846 reference pixels).
    /// Coordinates are top-left origin in reference image space.
    /// </summary>
    public static class TournamentPngLayout
    {
        public static float RefWidth => TournamentLayoutMetrics.RefWidth;
        public static float RefHeight => TournamentLayoutMetrics.RefHeight;

        public const float CardHeight = 174f;
        public const float CardGap = 4f;
        public const float FirstCardTop = 309f;
        public const float ScrollBottomPadding = 48f;

        private static readonly float[] CardTops = { 309f, 498f, 684f, 871f, 1053f, 1230f };

        /// <summary>Measured from turnamant1.png JOIN buttons (top-left origin), with touch padding.</summary>
        private static readonly Rect[] JoinRects =
        {
            new Rect(632f, 340f, 188f, 58f),   // 1 vs 1 Duel — PNG ~(647,352,163,35)
            new Rect(639f, 529f, 183f, 63f),   // Quick Cup — PNG ~(654,541,157,43)
            new Rect(632f, 715f, 188f, 58f),   // Mega Clash — PNG ~(647,727,162,37)
            new Rect(632f, 897f, 188f, 58f),   // Grand Clash — PNG ~(647,909,162,38)
            new Rect(637f, 1078f, 185f, 55f),  // Championship — PNG ~(652,1090,159,31)
            new Rect(632f, 1254f, 188f, 58f)   // World Cup — PNG ~(647,1266,161,35)
        };

        public static readonly Rect Back = new Rect(13f, 15f, 69f, 84f);
        public static readonly Rect WalletMask = new Rect(628f, 10f, 210f, 102f);
        public static readonly Rect Wallet = new Rect(634f, 14f, 200f, 91f);
        public static readonly Rect Deposit = new Rect(548f, 14f, 82f, 91f);

        public static float GetCardTop(int index)
        {
            if (index >= 0 && index < CardTops.Length)
                return CardTops[index];
            return FirstCardTop + index * (CardHeight + CardGap);
        }

        public static Rect GetJoinRect(int index)
        {
            if (index >= 0 && index < JoinRects.Length)
                return JoinRects[index];
            float top = GetCardTop(index);
            return new Rect(632f, top + 43f, 188f, 58f);
        }

        public static void PlaceFromTopLeft(RectTransform rt, Rect rect)
        {
            rt.anchorMin = new Vector2(rect.x / RefWidth, 1f - (rect.y + rect.height) / RefHeight);
            rt.anchorMax = new Vector2((rect.x + rect.width) / RefWidth, 1f - rect.y / RefHeight);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        public static int OverlayFont(float sizeAtRef) =>
            Mathf.Max(11, Mathf.RoundToInt(sizeAtRef * TournamentLayoutMetrics.Scale));
    }

    public class TournamentPageResponsive : MonoBehaviour
    {
        private RectTransform _scroll;
        private RectTransform _content;
        private float _lastW;
        private float _lastH;

        public void Configure(RectTransform scroll, RectTransform content)
        {
            _scroll = scroll;
            _content = content;
            Apply();
        }

        private void OnEnable() => Apply();

        private void Update() => TryRefresh();

        private void TryRefresh()
        {
            if (Mathf.Approximately(_lastW, Screen.width) && Mathf.Approximately(_lastH, Screen.height)) return;
            Apply();
        }

        public void Apply()
        {
            TournamentLayoutMetrics.Refresh();
            _lastW = Screen.width;
            _lastH = Screen.height;

            if (_scroll)
            {
                float left = TournamentLayoutMetrics.SafeInsetRef.x;
                float bottom = TournamentLayoutMetrics.SafeInsetRef.y;
                float right = TournamentLayoutMetrics.SafeInsetRef.z;
                float top = TournamentLayoutMetrics.SafeInsetRef.w;
                _scroll.offsetMin = new Vector2(left, bottom);
                _scroll.offsetMax = new Vector2(-right, -top);
            }

            if (_content)
                _content.sizeDelta = new Vector2(
                    TournamentLayoutMetrics.RefWidth,
                    TournamentLayoutMetrics.RefHeight + TournamentPngLayout.ScrollBottomPadding);
        }
    }
}
