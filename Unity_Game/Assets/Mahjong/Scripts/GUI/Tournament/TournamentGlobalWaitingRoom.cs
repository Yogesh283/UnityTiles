using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Full-screen waiting room overlay that persists across scenes while matchmaking.
    /// </summary>
    public class TournamentGlobalWaitingRoom : MonoBehaviour
    {
        private const int SortOrder = 9600;

        private static TournamentGlobalWaitingRoom instance;

        private TournamentWaitingRoomPanel panel;

        public static bool IsVisible =>
            instance != null && instance.panel != null && instance.panel.IsShowing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static TournamentWaitingRoomPanel EnsurePanel()
        {
            return EnsureInstance().panel;
        }

        public static Transform OverlayRoot => EnsureInstance().transform;

        public static void Show(TournamentDefinition tournament, Action onComplete)
        {
            TournamentGlobalWaitingRoom inst = EnsureInstance();
            if (!inst.gameObject.activeSelf)
                inst.gameObject.SetActive(true);

            Canvas canvas = inst.GetComponent<Canvas>();
            if (canvas != null && !canvas.enabled)
                canvas.enabled = true;

            EnsurePanel().Show(tournament, onComplete);
        }

        public static void Hide()
        {
            if (instance != null && instance.panel != null)
                instance.panel.Hide();
        }

        /// <summary>
        /// Tournament page open: remove leftover DDOL overlay from a prior matchmaking session.
        /// Returns true when a stale overlay was torn down.
        /// </summary>
        public static bool ClearStaleOnPageOpen()
        {
            if (!IsVisible)
                return false;

            if (TournamentJoinFlowGuard.IsActiveMatchmaking)
                return false;

            DestroyStaleOverlay();
            return true;
        }

        public static void DestroyStaleOverlay()
        {
            Hide();

            if (!instance)
                return;

            UnityEngine.Object.Destroy(instance.gameObject);
            instance = null;
        }

        private static TournamentGlobalWaitingRoom EnsureInstance()
        {
            if (instance) return instance;

            GameObject root = new GameObject(
                nameof(TournamentGlobalWaitingRoom),
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortOrder;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.pivot = new Vector2(0.5f, 0.5f);

            DontDestroyOnLoad(root);

            instance = root.AddComponent<TournamentGlobalWaitingRoom>();
            instance.panel = TournamentWaitingRoomPanel.Create(root.transform);
            return instance;
        }
    }
}
