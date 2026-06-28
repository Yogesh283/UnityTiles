using UnityEngine;
using UnityEngine.EventSystems;

namespace Mkey.Tournament
{
    /// <summary>
    /// Blocks ScrollRect drag capture so JOIN taps register inside ScrollRect.
    /// </summary>
    public class TournamentJoinHitArea : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData) { }
    }
}
