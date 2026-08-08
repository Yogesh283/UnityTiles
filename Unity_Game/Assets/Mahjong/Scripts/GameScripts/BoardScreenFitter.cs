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
        [Range(0f, 0.4f)] public float topReserveFraction = 0.12f;    // level / score / timer HUD
        [Range(0f, 0.4f)] public float bottomReserveFraction = 0.16f; // booster buttons row
        // Breathing room on each side, as a fraction of screen WIDTH.
        [Range(0f, 0.25f)] public float sideReserveFraction = 0.04f;
        // Only ever shrink the board to fit — never blow it up past its designed scale.
        public bool shrinkOnly = true;

        private Transform grid;
        private Camera cam;
        private Vector3 lastAppliedScale = Vector3.positiveInfinity;
        private int lastW;
        private int lastH;

        private void LateUpdate()
        {
            if (!EnsureRefs()) return;

            // Refit when the board was rebuilt (MatchGrid.SetScale resets localScale to the level's
            // designed scale) or when the screen size / orientation changed.
            bool scaleReset = grid.localScale != lastAppliedScale;
            bool screenChanged = Screen.width != lastW || Screen.height != lastH;
            if (!scaleReset && !screenChanged) return;

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

            float availH = worldH * (1f - topReserveFraction - bottomReserveFraction);
            float availW = worldW * (1f - 2f * sideReserveFraction);
            if (availH <= 0f || availW <= 0f) return;

            float fit = Mathf.Min(availW / bounds.size.x, availH / bounds.size.y);
            if (shrinkOnly) fit = Mathf.Min(fit, 1f);
            if (fit <= 0f) return;

            // Uniform shrink about the board's current pivot.
            grid.localScale *= fit;

            // Slide the (now correctly sized) board into the free band between the top HUD and the
            // bottom boosters, and centre it horizontally on the camera.
            Bounds after = MeasureTiles() ?? bounds;
            float bandCenterY =
                cam.transform.position.y + (bottomReserveFraction - topReserveFraction) * worldH * 0.5f;

            Vector3 pos = grid.position;
            pos.x += cam.transform.position.x - after.center.x;
            pos.y += bandCenterY - after.center.y;
            grid.position = pos;

            lastAppliedScale = grid.localScale;
            lastW = Screen.width;
            lastH = Screen.height;
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
