using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Shared safe-area helpers so the in-game HUD, timer badge, board fitter and booster row never
    /// hide under the Android status bar / notch / punch-hole / dynamic island (and iOS insets).
    ///
    /// Everything reads <see cref="Screen.safeArea"/> at runtime and converts the pixel insets into
    /// the units each consumer needs (canvas reference units for UI anchoredPositions, or a fraction
    /// of screen height for the world-space board fitter). On devices with no inset (and in the
    /// Editor) every value is 0, so the offset only appears where the hardware actually needs it.
    /// </summary>
    public static class WxoSafeArea
    {
        // Required Canvas Scaler reference (Scale With Screen Size).
        public const float RefW = 1080f;
        public const float RefH = 2400f;
        public const float Match = 0.5f;

        /// <summary>Unsafe strip at the TOP of the screen, in real pixels (status bar / notch).</summary>
        public static float TopInsetPixels()
        {
            Rect s = Screen.safeArea;
            float px = Screen.height - (s.y + s.height);
            return px > 1f ? px : 0f;
        }

        /// <summary>Unsafe strip at the BOTTOM of the screen, in real pixels (gesture / nav bar).</summary>
        public static float BottomInsetPixels()
        {
            float px = Screen.safeArea.y;
            return px > 1f ? px : 0f;
        }

        /// <summary>Top inset as a fraction of screen height (0..1) — for the world-space board.</summary>
        public static float TopInsetFraction()
        {
            return Screen.height > 0 ? Mathf.Clamp01(TopInsetPixels() / Screen.height) : 0f;
        }

        /// <summary>Bottom inset as a fraction of screen height (0..1) — for the world-space board.</summary>
        public static float BottomInsetFraction()
        {
            return Screen.height > 0 ? Mathf.Clamp01(BottomInsetPixels() / Screen.height) : 0f;
        }

        /// <summary>Top inset expressed in canvas reference units (what anchoredPosition uses).</summary>
        public static float TopInsetCanvas(Canvas root)
        {
            float sf = ScaleFactor(root);
            return sf > 0f ? TopInsetPixels() / sf : 0f;
        }

        /// <summary>Bottom inset expressed in canvas reference units.</summary>
        public static float BottomInsetCanvas(Canvas root)
        {
            float sf = ScaleFactor(root);
            return sf > 0f ? BottomInsetPixels() / sf : 0f;
        }

        /// <summary>Unsafe strip on the LEFT of the screen, in real pixels (landscape punch-hole).</summary>
        public static float LeftInsetPixels()
        {
            float px = Screen.safeArea.x;
            return px > 1f ? px : 0f;
        }

        /// <summary>Left inset expressed in canvas reference units.</summary>
        public static float LeftInsetCanvas(Canvas root)
        {
            float sf = ScaleFactor(root);
            return sf > 0f ? LeftInsetPixels() / sf : 0f;
        }

        /// <summary>Pixels → fraction of screen height.</summary>
        public static float PixelsToHeightFraction(float pixels)
        {
            return Screen.height > 0 ? Mathf.Clamp01(pixels / Screen.height) : 0f;
        }

        /// <summary>The canvas' px-per-unit. Uses the live scaleFactor when available, otherwise
        /// reproduces the "Scale With Screen Size" (match 0.5) formula so callers work before the
        /// CanvasScaler has run.</summary>
        private static float ScaleFactor(Canvas root)
        {
            if (root && root.scaleFactor > 0.0001f) return root.scaleFactor;
            if (Screen.width <= 0 || Screen.height <= 0) return 1f;
            float logW = Mathf.Log(Screen.width / RefW, 2f);
            float logH = Mathf.Log(Screen.height / RefH, 2f);
            return Mathf.Pow(2f, Mathf.Lerp(logW, logH, Match));
        }

        /// <summary>Forces the required Canvas Scaler config on a HUD canvas: Scale With Screen Size,
        /// 1080×2400 reference, match 0.5. No-op if the canvas has no scaler.</summary>
        public static void ConfigureScaler(Canvas root)
        {
            if (!root) return;
            CanvasScaler cs = root.GetComponent<CanvasScaler>();
            if (!cs) return;
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(RefW, RefH);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = Match;
        }

        /// <summary>
        /// Applies <see cref="Screen.safeArea"/> as offsets on a full-stretch RectTransform so its
        /// children (header / footer) sit inside the safe rectangle. Uses the root canvas scale.
        /// </summary>
        public static void ApplySafeAreaInsets(RectTransform rt, Canvas root)
        {
            if (!rt) return;
            float top = TopInsetCanvas(root);
            float bottom = BottomInsetCanvas(root);
            float left = LeftInsetCanvas(root);
            // Right inset = screen width - (safe.x + safe.width)
            Rect s = Screen.safeArea;
            float rightPx = Screen.width - (s.x + s.width);
            float sf = ScaleFactor(root);
            float right = (sf > 0f && rightPx > 1f) ? rightPx / sf : 0f;

            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
