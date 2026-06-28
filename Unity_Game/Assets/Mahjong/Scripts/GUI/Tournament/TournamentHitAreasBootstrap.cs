using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Ensures interactive hit areas render above scroll/wallet layers for reliable raycasts.
    /// </summary>
    public class TournamentHitAreasBootstrap : MonoBehaviour
    {
        private void Start()
        {
            transform.SetAsLastSibling();

            Transform firstJoin = transform.Find("Join_duel_1v1");
            if (firstJoin)
                firstJoin.SetAsLastSibling();

            Transform lastJoin = transform.Find("Join_world_cup");
            if (lastJoin)
                lastJoin.SetAsLastSibling();
        }
    }
}
