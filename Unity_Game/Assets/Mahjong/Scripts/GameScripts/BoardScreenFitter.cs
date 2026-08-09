using UnityEngine;

namespace Mkey
{
    /// <summary>
    /// Scales &amp; recenters the tile board so it always fits inside the phone screen, between the top
    /// HUD (level / score / timer) and the bottom booster row, instead of overflowing under them on
    /// tall aspect ratios. The scale is uniform, so tiles are never distorted — the board just gets
    /// smaller and slots into the free band. Play mode only; the level constructor (Edit mode) keeps
    /// the raw designed layout. Added at runtime by <see cref="GameBoard"/>.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class BoardScreenFitter : MonoBehaviour
    {
        // Fractions of screen HEIGHT reserved for the on-screen chrome so tiles never sit under it.
        // Increase topReserve to push the board lower (more room for the header), increase
        // bottomReserve to lift it off the booster buttons. These two are the main tuning knobs.
        [Range(0f, 0.4f)] public float topReserveFraction = 0.15f;    // level / score / timer HUD
        [Range(0f, 0.4f)] public float bottomReserveFraction = 0.24f; // booster buttons row
        // Breathing room on each side, as a fraction of screen WIDTH.
        [Range(0f, 0.25f)] public float sideReserveFraction = 0.04f;
        // Only ever shrink the board to fit — never blow it up past its designed scale.
        public bool shrinkOnly = true;

        private Transform grid;
        private Camera cam;
        private Vector3 lastAppliedScale = Vector3.positiveInfinity;
        private int lastW;
        private int lastH;
        private float lastTopInset = -1f;
        private float lastBottomInset = -1f;

        private void LateUpdate()
        {
            if (!EnsureRefs()) return;

            // Refit when the board was rebuilt (MatchGrid.SetScale resets localScale to the level's
            // designed scale), when the screen size / orientation changed, or when the device safe
            // area changed (notch device, rotation) so the board stays clear of the top HUD.
            bool scaleReset = grid.localScale != lastAppliedScale;
            bool screenChanged = Screen.width != lastW || Screen.height != lastH;
            bool insetChanged =
                !Mathf.Approximately(WxoSafeArea.TopInsetFraction(), lastTopInset) ||
                !Mathf.Approximately(WxoSafeArea.BottomInsetFraction(), lastBottomInset);
            if (!scaleReset && !screenChanged && !insetChanged) return;

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
            Bounds? measured = MeasureTiles();
            if (measured == null) return; // tiles not built yet

            Bounds bounds = measured.Value;
            if (bounds.size.x <= 0f || bounds.size.y <= 0f) return;

            float worldH = cam.orthographicSize * 2f;
            float worldW = worldH * cam.aspect;

            // Add the device safe-area insets on top of the designed reserves so the board drops below
            // the notch/status-bar HUD and lifts above the gesture/nav bar. Both are 0 on devices
            // without insets, so the designed layout is unchanged there.
            float topInset = WxoSafeArea.TopInsetFraction();
            float bottomInset = WxoSafeArea.BottomInsetFraction();
            float topReserve = topReserveFraction + topInset;
            float bottomReserve = bottomReserveFraction + bottomInset;

            float availH = worldH * (1f - topReserve - bottomReserve);
            float availW = worldW * (1f - 2f * sideReserveFraction);
            if (availH <= 0f || availW <= 0f) return;

            float fit = Mathf.Min(availW / bounds.size.x, availH / bounds.size.y);
            if (shrinkOnly) fit = Mathf.Min(fit, 1f);
            if (fit <= 0f) return;

            // Uniform shrink about the board's current pivot.
            grid.localScale *= fit;

            // Slide the (now correctly sized) board into the free band between the top HUD and the
            // bottom boosters, and centre it horizontally on the camera. Uses the safe-area-adjusted
            // reserves so the band (and its centre) shifts down with the HUD on notched devices.
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
