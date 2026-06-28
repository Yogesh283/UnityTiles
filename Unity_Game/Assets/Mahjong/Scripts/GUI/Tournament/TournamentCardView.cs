using System;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Holds one tournament's data and binds its JOIN button to that definition.
    /// </summary>
    public class TournamentCardView : MonoBehaviour
    {
        private TournamentDefinition data;

        public TournamentDefinition Data => data;

        public void Setup(TournamentDefinition definition, Action<TournamentDefinition> joinCallback, float animDelay, int index, Transform hitParent)
        {
            data = definition;
        }

        public void Setup(TournamentDefinition definition, Action<TournamentDefinition> joinCallback, float animDelay, int index)
        {
            Setup(definition, joinCallback, animDelay, index, null);
        }

        public void Setup(TournamentDefinition definition, Action<TournamentDefinition> joinCallback, float animDelay)
        {
            Setup(definition, joinCallback, animDelay, 0, null);
        }

        public void BindJoinButton(Transform hitAreas, Rect joinRect, Action<TournamentDefinition> joinCallback)
        {
            if (data == null)
            {
                Debug.LogError("[TournamentCardView] Cannot bind JOIN — TournamentDefinition is null");
                return;
            }

            TournamentUIFactory.CreateJoinButton(hitAreas, data, joinRect, joinCallback);
        }
    }
}
