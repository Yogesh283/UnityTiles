using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Ensures interactive hit areas render above scroll/wallet layers for reliable raycasts.
    /// </summary>
    public class TournamentHitAreasBootstrap : MonoBehaviour
    {
        private const int HitAreasSortOrder = 120;

        private void Awake() => Apply();

        private void Start() => Apply();

        public static void EnsureOnTop(Transform hitAreas)
        {
            if (!hitAreas)
                return;

            TournamentHitAreasBootstrap bootstrap = hitAreas.GetComponent<TournamentHitAreasBootstrap>();
            if (!bootstrap)
                bootstrap = hitAreas.gameObject.AddComponent<TournamentHitAreasBootstrap>();
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
            canvas.sortingOrder = HitAreasSortOrder;

            transform.SetAsLastSibling();
        }
    }
}
