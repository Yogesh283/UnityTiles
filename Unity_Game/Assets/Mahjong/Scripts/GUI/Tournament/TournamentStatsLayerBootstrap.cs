using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Keeps stat values (Players, Entry Fee, etc.) drawn above HitAreas and ScrollCatcher.
    /// </summary>
    public class TournamentStatsLayerBootstrap : MonoBehaviour
    {
        private const int StatsSortOrder = 130;

        private void Awake() => Apply();

        public static void EnsureVisible(Transform statsLayer)
        {
            if (!statsLayer)
                return;

            TournamentStatsLayerBootstrap bootstrap = statsLayer.GetComponent<TournamentStatsLayerBootstrap>();
            if (!bootstrap)
                bootstrap = statsLayer.gameObject.AddComponent<TournamentStatsLayerBootstrap>();
            bootstrap.Apply();
        }

        private void Apply()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (!canvas)
            {
                canvas = gameObject.AddComponent<Canvas>();
                gameObject.AddComponent<GraphicRaycaster>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = StatsSortOrder;

            CanvasGroup group = GetComponent<CanvasGroup>();
            if (!group)
                group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }
}
