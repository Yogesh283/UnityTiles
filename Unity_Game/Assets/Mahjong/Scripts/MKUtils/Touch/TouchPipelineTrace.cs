using UnityEngine;
using UnityEngine.EventSystems;
using Mkey.Shell;

namespace Mkey
{
    /// <summary>
    /// Development touch-pipeline tracer. Console only — no on-screen UI.
    /// </summary>
    public static class TouchPipelineTrace
    {
        public static int PhysicalTaps;
        public static int PointerDowns;
        public static int PointerUps;
        public static int RaycastHits;
        public static int ValidTileHits;
        public static int SelectionsAccepted;
        public static int SelectionsRejected;

        public static void ResetCounters()
        {
            PhysicalTaps = PointerDowns = PointerUps = 0;
            RaycastHits = ValidTileHits = SelectionsAccepted = SelectionsRejected = 0;
        }

        public static void LogSummary(string tag)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log(
                $"[TouchPipeline] SUMMARY {tag} physical={PhysicalTaps} down={PointerDowns} up={PointerUps} " +
                $"hits={RaycastHits} validTile={ValidTileHits} accepted={SelectionsAccepted} rejected={SelectionsRejected}");
#endif
        }

        public static void LogFail(string failPoint, string detail = "")
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogWarning($"[TouchPipeline] FAIL_POINT={failPoint} {detail}");
#endif
            // Always mirror short fail line to logcat in shell so release APKs can be diagnosed.
            if (MatchIQShellBridge.IsActive)
                Debug.LogWarning($"[TouchPipeline] FAIL_POINT={failPoint} {detail}");
        }

        public static void LogTap(
            string phase,
            Vector2 screen,
            Vector3 world,
            Camera cam,
            int hits,
            string topHit,
            string selectable,
            string selection,
            string failPoint,
            string extra = "")
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            string camInfo = cam
                ? $"{cam.name} pos={cam.transform.position} ortho={cam.orthographicSize} rect={cam.rect}"
                : "NULL";
            Debug.Log(
                $"[TouchPipeline]\n" +
                $"phase={phase}\n" +
                $"screen=({screen.x:F1},{screen.y:F1})\n" +
                $"unityScreen=({Screen.width}x{Screen.height})\n" +
                $"safeArea={Screen.safeArea}\n" +
                $"world=({world.x:F3},{world.y:F3},{world.z:F3})\n" +
                $"camera={camInfo}\n" +
                $"hits={hits} topHit={topHit} selectable={selectable}\n" +
                $"selection={selection}\n" +
                $"FAIL_POINT={failPoint}\n" +
                $"{extra}");
#endif
        }
    }
}
