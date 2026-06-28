using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Keeps the tournament scene camera on the map green color.
    /// </summary>
    public class TournamentGreenCameraBg : MonoBehaviour
    {
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Apply()
        {
            if (!cam) cam = GetComponent<Camera>();
            if (!cam) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = TournamentPremiumTheme.EmeraldDark;
        }
    }
}
