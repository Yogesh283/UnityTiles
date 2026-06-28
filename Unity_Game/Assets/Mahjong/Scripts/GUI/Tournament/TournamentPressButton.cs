using UnityEngine;
using UnityEngine.EventSystems;

namespace Mkey.Tournament
{
    public class TournamentPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float pressedScale = 0.95f;

        private Vector3 baseScale;
        private bool pressed;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            transform.localScale = baseScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            transform.localScale = baseScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!pressed) return;
            pressed = false;
            transform.localScale = baseScale;
        }
    }
}
