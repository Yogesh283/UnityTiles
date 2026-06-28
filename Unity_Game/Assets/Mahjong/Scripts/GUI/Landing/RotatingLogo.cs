using UnityEngine;

namespace Mkey
{
    /// <summary>
    /// Legacy component — rotation disabled; logo stays static.
    /// </summary>
    public class RotatingLogo : MonoBehaviour
    {
        private void OnEnable()
        {
            StopRotation();
        }

        public void StartRotation()
        {
            StopRotation();
        }

        public void StopRotation()
        {
            SimpleTween.Cancel(gameObject, false);
            RectTransform rt = GetComponent<RectTransform>();
            if (rt) rt.localRotation = Quaternion.identity;
        }
    }
}
