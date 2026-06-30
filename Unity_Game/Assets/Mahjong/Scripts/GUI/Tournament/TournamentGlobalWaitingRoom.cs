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
        private const int SortOrder = 4500;

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

        public static void Show(TournamentDefinition tournament, Action onComplete)
        {
            EnsurePanel().Show(tournament, onComplete);
        }

        public static void Hide()
        {
            if (instance != null && instance.panel != null)
                instance.panel.Hide();
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

            DontDestroyOnLoad(root);
            instance = root.AddComponent<TournamentGlobalWaitingRoom>();
            instance.panel = TournamentWaitingRoomPanel.Create(root.transform);
            return instance;
        }
    }
}
