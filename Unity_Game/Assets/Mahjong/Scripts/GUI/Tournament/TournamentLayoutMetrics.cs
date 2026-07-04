using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Scales tournament UI from PNG reference (862×1825) and applies safe-area insets.
    /// </summary>
    public static class TournamentLayoutMetrics
    {
        // Native pixel size of Resources/Tournament/turnamant1.png. All hit-area coordinates below
        // are measured against these dimensions, so keep them in sync with the actual PNG.
        public const float RefWidth = 853f;
        public const float RefHeight = 1844f;

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
    /// Hit areas aligned to turnamant1.png (862×1825 reference pixels).
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

        /// <summary>PNG card order — always map tournament id to layout index (API order may differ).</summary>
        private static readonly string[] CardTournamentIds =
        {
            "duel_1v1",
            "quick_cup",
            "mega_clash",
            "grand_clash",
            "championship",
            "world_cup"
        };

        /// <summary>Measured from turnamant1.png JOIN buttons (top-left origin), with touch padding.</summary>
        // Green JOIN button rects, measured by pixel-scanning turnamant1.png (854×1842). Each rect is
        // centered on its baked green button so the JOIN/FULL label sits inside the button.
        private static readonly Rect[] JoinRects =
        {
            new Rect(639f, 486f, 154f, 60f),    // 1 vs 1 Duel
            new Rect(639f, 679f, 154f, 60f),    // Quick Cup
            new Rect(639f, 868f, 154f, 60f),    // Mega Clash
            new Rect(639f, 1054f, 154f, 60f),   // Grand Clash
            new Rect(639f, 1236f, 154f, 60f),   // Championship
            new Rect(639f, 1416f, 154f, 60f)    // World Cup
        };

        /// <summary>Back arrow circle on turnamant1.png (top-left). Enlarged for a reliable touch target.</summary>
        public static readonly Rect Back = new Rect(20f, 22f, 140f, 140f);
        /// <summary>Black balance interior on turnamant1.png (right header).</summary>
        public static readonly Rect Wallet = new Rect(602f, 72f, 258f, 26f);
        /// <summary>Gold + deposit hit area (left of wallet).</summary>
        public static readonly Rect Deposit = new Rect(448f, 8f, 154f, 90f);

        public const float CardStatsRowHeight = 28f;
        public const float CardStatColumnWidth = 92f;
        /// <summary>Column centers on turnamant1.png — Players, Entry Fee, Prize Pool, Top Win.</summary>
        private static readonly float[] CardStatColumnCenterX = { 221f, 329f, 437f, 545f };
        /// <summary>Pixels below each card top to the value row center (measured on turnamant1.png).</summary>
        private static readonly float[] CardStatOffsetFromCardTop = { 178f, 154f, 132f, 105f, 86f, 70f };

        public static Rect GetCardStatRect(int column, int cardIndex)
        {
            float centerY = GetCardTop(cardIndex) + (
                cardIndex >= 0 && cardIndex < CardStatOffsetFromCardTop.Length
                    ? CardStatOffsetFromCardTop[cardIndex]
                    : 130f);
            float centerX = column >= 0 && column < CardStatColumnCenterX.Length
                ? CardStatColumnCenterX[column]
                : CardStatColumnCenterX[0];
            return new Rect(
                centerX - CardStatColumnWidth * 0.5f,
                centerY - CardStatsRowHeight * 0.5f,
                CardStatColumnWidth,
                CardStatsRowHeight);
        }

        public static Rect GetCardStatRect(int column, float cardTop)
        {
            int index = 0;
            float best = float.MaxValue;
            for (int i = 0; i < CardTops.Length; i++)
            {
                float d = Mathf.Abs(CardTops[i] - cardTop);
                if (d < best)
                {
                    best = d;
                    index = i;
                }
            }

            return GetCardStatRect(column, index);
        }

        public static float GetCardTop(int index)
        {
            if (index >= 0 && index < CardTops.Length)
                return CardTops[index];
            return FirstCardTop + index * (CardHeight + CardGap);
        }

        public static int GetCardIndexForTournament(string tournamentId)
        {
            if (string.IsNullOrEmpty(tournamentId))
                return -1;

            for (int i = 0; i < CardTournamentIds.Length; i++)
            {
                if (CardTournamentIds[i] == tournamentId)
                    return i;
            }

            return -1;
        }

        /// <summary>862×1825 pixel layer aligned with turnamant1.png on PageLayer.</summary>
        public static RectTransform CreatePagePixelLayer(RectTransform pageLayer, string name)
        {
            RectTransform layer = TournamentUIFactory.CreateRect(pageLayer, name);
            layer.anchorMin = new Vector2(0.5f, 1f);
            layer.anchorMax = new Vector2(0.5f, 1f);
            layer.pivot = new Vector2(0.5f, 1f);
            layer.sizeDelta = new Vector2(RefWidth, RefHeight);
            layer.anchoredPosition = Vector2.zero;
            return layer;
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
            // Top-left pixel coords on the 862×1825 page (parent must be PageLayer-sized).
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(rect.width, rect.height);
            rt.anchoredPosition = new Vector2(
                rect.x + rect.width * 0.5f,
                -(rect.y + rect.height * 0.5f));
        }

        /// <summary>Fractional placement for full-stretch parents (join buttons, wallet, stats).</summary>
        public static void PlaceFromTopLeftAnchored(RectTransform rt, Rect rect)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(rect.x / RefWidth, 1f - (rect.y + rect.height) / RefHeight);
            rt.anchorMax = new Vector2((rect.x + rect.width) / RefWidth, 1f - rect.y / RefHeight);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
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
