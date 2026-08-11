using UnityEngine;
using Mkey.Shell;

namespace Mkey
{
    /// <summary>
    /// Keeps the gameplay background covering the orthographic camera without distortion
    /// (uniform scale-to-cover). Disables the old keyed aspect scaler. Does not touch tiles.
    /// </summary>
    [DefaultExecutionOrder(40)]
    public class GameBackgroundCover : MonoBehaviour
    {
        private SpriteRenderer bg;
        private Camera cam;
        private bool keyedDisabled;

        private void LateUpdate()
        {
            if (!EnsureRefs()) return;
            if (!bg.sprite) return;

            if (!keyedDisabled)
            {
                var keyed = bg.GetComponent<ImageAspectRatioBehavior>();
                if (keyed && keyed.enabled) keyed.enabled = false;
                keyedDisabled = true;
            }

            // Centre on camera
            Vector3 p = bg.transform.position;
            bg.transform.position = new Vector3(
                cam.transform.position.x,
                cam.transform.position.y,
                p.z);

            // Uniform cover: fill the view without stretching X/Y independently.
            float worldH = cam.orthographicSize * 2f;
            float worldW = worldH * cam.aspect;
            Bounds b = bg.sprite.bounds;
            float sx = worldW / Mathf.Max(0.0001f, b.size.x);
            float sy = worldH / Mathf.Max(0.0001f, b.size.y);
            float s = Mathf.Max(sx, sy);
            bg.transform.localScale = new Vector3(s, s, 1f);
        }

        private bool EnsureRefs()
        {
            if (!bg)
            {
                GameBoard board = GameBoard.Instance ? GameBoard.Instance : FindFirstObjectByType<GameBoard>();
                if (board) bg = board.backGround;
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

            return bg && cam && cam.orthographic;
        }
    }
}
