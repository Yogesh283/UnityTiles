using UnityEngine;

namespace Mkey
{
    /// <summary>
    /// Scales &amp; recenters the tile board so it fits between the RN top HUD and bottom boosters.
    /// Always measures at a captured base scale, then applies an absolute fit — never compounds.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class BoardScreenFitter : MonoBehaviour
    {
        [Range(0f, 0.45f)] public float topReserveFraction = 0.17f;
        [Range(0f, 0.45f)] public float bottomReserveFraction = 0.23f;
        [Range(0f, 0.25f)] public float sideReserveFraction = 0.05f;
        public bool shrinkOnly = true;

        private const float MinGapBelowHudPx = 100f;
        private const float MinGapAboveBottomPx = 180f;
        private const float HudChromePx = 220f;
        private const float BottomChromePx = 240f;

        private Transform grid;
        private Camera cam;
        private Vector3 baseScale = Vector3.one;
        private bool capturedBase;
        private Vector3 lastAppliedScale = Vector3.positiveInfinity;
        private int lastW;
        private int lastH;
        private float lastTopInset = -1f;
        private float lastBottomInset = -1f;

        private void LateUpdate()
        {
            if (!EnsureRefs()) return;

            bool screenChanged = Screen.width != lastW || Screen.height != lastH;
            bool insetChanged =
                !Mathf.Approximately(WxoSafeArea.TopInsetFraction(), lastTopInset) ||
                !Mathf.Approximately(WxoSafeArea.BottomInsetFraction(), lastBottomInset);
            // Only refit when screen/insets change, or the first time (lastAppliedScale sentinel).
            bool firstFit = lastAppliedScale.x == Mathf.Infinity;
            if (!firstFit && !screenChanged && !insetChanged) return;

            Fit();
        }

        private bool EnsureRefs()
        {
            if (!grid)
            {
                GameBoard board = GameBoard.Instance;
                if (board) grid = board.GridContainer;
            }

            if (!cam || !cam.orthographic)
            {
                cam = Camera.main;
                if (!cam || !cam.orthographic)
                {
                    foreach (Camera c in Camera.allCameras)
                    {
                        if (c && c.orthographic && c.isActiveAndEnabled)
                        {
                            cam = c;
                            break;
                        }
                    }
                }
            }

            return grid && cam && cam.orthographic;
        }

        private void Fit()
        {
            if (!capturedBase)
            {
                baseScale = grid.localScale;
                if (baseScale.x <= 0.0001f || baseScale.y <= 0.0001f)
                    baseScale = Vector3.one;
                capturedBase = true;
            }

            // Always measure at the original authored scale so fit never compounds.
            grid.localScale = baseScale;

            Bounds? measured = MeasureTiles();
            if (measured == null) return;

            Bounds bounds = measured.Value;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f) return;

            float worldH = cam.orthographicSize * 2f;
            float worldW = worldH * cam.aspect;

            float topInset = WxoSafeArea.TopInsetFraction();
            float bottomInset = WxoSafeArea.BottomInsetFraction();

            float topPx = WxoSafeArea.TopInsetPixels() + HudChromePx + MinGapBelowHudPx;
            float botPx = WxoSafeArea.BottomInsetPixels() + BottomChromePx + MinGapAboveBottomPx;
            float topFromPx = WxoSafeArea.PixelsToHeightFraction(topPx);
            float botFromPx = WxoSafeArea.PixelsToHeightFraction(botPx);

            float topReserve = Mathf.Max(topReserveFraction + topInset, topFromPx);
            float bottomReserve = Mathf.Max(bottomReserveFraction + bottomInset, botFromPx);

            if (topReserve + bottomReserve > 0.78f)
            {
                float scale = 0.78f / (topReserve + bottomReserve);
                topReserve *= scale;
                bottomReserve *= scale;
            }

            float availH = worldH * (1f - topReserve - bottomReserve);
            float availW = worldW * (1f - 2f * sideReserveFraction);
            if (availH <= 0f || availW <= 0f) return;

            float fit = Mathf.Min(availW / bounds.size.x, availH / bounds.size.y);
            if (shrinkOnly) fit = Mathf.Min(fit, 1f);
            if (fit <= 0f) return;

            grid.localScale = baseScale * fit;

            Bounds after = MeasureTiles() ?? bounds;
            float bandCenterY =
                cam.transform.position.y + (bottomReserve - topReserve) * worldH * 0.5f;

            Vector3 pos = grid.position;
            pos.x += cam.transform.position.x - after.center.x;
            pos.y += bandCenterY - after.center.y;
            grid.position = pos;

            lastAppliedScale = grid.localScale;
            lastW = Screen.width;
            lastH = Screen.height;
            lastTopInset = topInset;
            lastBottomInset = bottomInset;
        }

        private Bounds? MeasureTiles()
        {
            SpriteRenderer[] renderers = grid.GetComponentsInChildren<SpriteRenderer>(false);
            bool has = false;
            Bounds bounds = new Bounds();
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer r = renderers[i];
                if (!r || !r.enabled) continue;
                if (!has)
                {
                    bounds = r.bounds;
                    has = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return has ? bounds : (Bounds?)null;
        }
    }
}
