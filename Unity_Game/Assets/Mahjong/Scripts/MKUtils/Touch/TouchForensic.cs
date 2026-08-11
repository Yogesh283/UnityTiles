using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mkey.Shell;

namespace Mkey
{
    /// <summary>
    /// Forensic touch pipeline tracer (MATCHIQ FINAL TOUCH FORENSIC DEBUG).
    /// Console / logcat only for release shell; visual markers only in DEVELOPMENT_BUILD / EDITOR.
    /// Does not change selection behavior — diagnostics only.
    /// </summary>
    public static class TouchForensic
    {
        public static int NextPressId;

        // 50-tap counters (reset via ResetSession)
        public static int RawReceived;
        public static int PointerDownReceived;
        public static int TouchPadReceived;
        public static int RaycastHit;
        public static int ValidTile;
        public static int FreeTile;
        public static int SelectionSuccess;
        public static int FailInactive;
        public static int FailNoPointer;
        public static int FailRaycastMiss;
        public static int FailNotFree;
        public static int FailPressHandled;
        public static int FailSelectionState;
        public static int FailOther;

        public static void ResetSession()
        {
            RawReceived = PointerDownReceived = TouchPadReceived = 0;
            RaycastHit = ValidTile = FreeTile = SelectionSuccess = 0;
            FailInactive = FailNoPointer = FailRaycastMiss = FailNotFree = 0;
            FailPressHandled = FailSelectionState = FailOther = 0;
        }

        public static int BeginPress()
        {
            NextPressId++;
            return NextPressId;
        }

        public static void Trace(int pressId, string stage, string detail = "")
        {
            string line = string.IsNullOrEmpty(detail)
                ? $"[TOUCH_TRACE] PRESS_ID={pressId} STAGE={stage}"
                : $"[TOUCH_TRACE] PRESS_ID={pressId} STAGE={stage} {detail}";
            // Shell APKs need logcat; also keep editor/dev.
            if (MatchIQShellBridge.IsActive)
                Debug.Log(line);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            else
                Debug.Log(line);
#endif
        }

        public static void Fail(int pressId, string reason, string detail = "")
        {
            switch (reason)
            {
                case "TOUCHPAD_INACTIVE": FailInactive++; break;
                case "PointerDown_NOT_RECEIVED":
                case "NO_POINTER_EVENT": FailNoPointer++; break;
                case "RAYCAST_MISS":
                case "NO_HIT": FailRaycastMiss++; break;
                case "TILE_NOT_SELECTABLE":
                case "NOT_FREE": FailNotFree++; break;
                case "HANDLED_THIS_PRESS":
                case "PRESS_ALREADY_HANDLED": FailPressHandled++; break;
                case "SELECTION_STATE": FailSelectionState++; break;
                default: FailOther++; break;
            }

            string line = string.IsNullOrEmpty(detail)
                ? $"[TOUCH_TRACE] PRESS_ID={pressId} STAGE=FAIL reason={reason}"
                : $"[TOUCH_TRACE] PRESS_ID={pressId} STAGE=FAIL reason={reason} {detail}";
            if (MatchIQShellBridge.IsActive)
                Debug.LogWarning(line);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            else
                Debug.LogWarning(line);
#endif
            // Bridge legacy fail tag for existing filters.
            TouchPipelineTrace.LogFail(reason, $"PRESS_ID={pressId} {detail}");
        }

        public static void LogSummary()
        {
            string s =
                $"[TOUCH_TRACE] SUMMARY raw={RawReceived} pointerDown={PointerDownReceived} " +
                $"touchPad={TouchPadReceived} raycast={RaycastHit} validTile={ValidTile} " +
                $"free={FreeTile} selected={SelectionSuccess} " +
                $"failInactive={FailInactive} failNoPointer={FailNoPointer} " +
                $"failRaycast={FailRaycastMiss} failNotFree={FailNotFree} " +
                $"failHandled={FailPressHandled} failSelState={FailSelectionState} failOther={FailOther}";
            Debug.Log(s);
        }

        public static string DumpColliders(Vector2 worldXY)
        {
            Collider2D[] all = Physics2D.OverlapPointAll(worldXY);
            if (all == null || all.Length == 0)
                return "hits=0";

            var sb = new StringBuilder();
            sb.Append("hits=").Append(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                Collider2D c = all[i];
                if (!c) { sb.Append($" [{i}]=null"); continue; }
                MahjongTile t = c.GetComponentInParent<MahjongTile>();
                SpriteRenderer sr = t && t.SRenderer ? t.SRenderer : c.GetComponentInParent<SpriteRenderer>();
                int sort = sr ? sr.sortingOrder : -1;
                sb.Append(
                    $" [{i}] go={c.gameObject.name} layer={c.gameObject.layer} " +
                    $"enabled={c.enabled} active={c.gameObject.activeInHierarchy} " +
                    $"z={c.transform.position.z:F3} sort={sort} " +
                    $"tile={(t ? t.name : "no")} tileLayer={(t ? t.Layer : -1)}");
                if (t && t.SRenderer && t.boxCollider)
                {
                    Bounds sbnds = t.SRenderer.bounds;
                    Bounds cbnds = t.boxCollider.bounds;
                    sb.Append(
                        $" spriteBounds=({sbnds.center.x:F2},{sbnds.center.y:F2},{sbnds.size.x:F2}x{sbnds.size.y:F2})" +
                        $" collBounds=({cbnds.center.x:F2},{cbnds.center.y:F2},{cbnds.size.x:F2}x{cbnds.size.y:F2})" +
                        $" scale={t.transform.lossyScale}");
                }
            }
            return sb.ToString();
        }
    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    /// <summary>Tiny on-screen markers: raw screen (yellow), world (cyan), collider (magenta). Never in release.</summary>
    public class TouchForensicMarkers : MonoBehaviour
    {
        private static TouchForensicMarkers instance;
        private Vector2 rawScreen;
        private Vector3 worldPos;
        private Vector3 colliderPos;
        private bool show;
        private float hideAt;

        public static void Show(Vector2 screen, Vector3 world, Vector3 collider)
        {
            if (!instance)
            {
                var go = new GameObject("TouchForensicMarkers");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<TouchForensicMarkers>();
            }
            instance.rawScreen = screen;
            instance.worldPos = world;
            instance.colliderPos = collider;
            instance.show = true;
            instance.hideAt = Time.unscaledTime + 1.2f;
        }

        private void OnGUI()
        {
            if (!show) return;
            if (Time.unscaledTime > hideAt) { show = false; return; }

            // A: raw screen
            DrawScreenDot(rawScreen, Color.yellow, 14);
            // B: world → screen
            Camera cam = Camera.main;
            if (cam)
            {
                Vector3 ws = cam.WorldToScreenPoint(worldPos);
                DrawScreenDot(new Vector2(ws.x, ws.y), Color.cyan, 10);
                Vector3 cs = cam.WorldToScreenPoint(colliderPos);
                DrawScreenDot(new Vector2(cs.x, cs.y), Color.magenta, 8);
            }
        }

        private static void DrawScreenDot(Vector2 screen, Color c, float size)
        {
            // GUI y is top-down; Unity screen y is bottom-up.
            float guiY = Screen.height - screen.y;
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(screen.x - size * 0.5f, guiY - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
#endif
}
