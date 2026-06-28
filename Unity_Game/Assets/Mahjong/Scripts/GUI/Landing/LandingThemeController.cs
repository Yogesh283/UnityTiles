using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Landing page: dark intro with static logo, then reveals the green map theme.
    /// </summary>
    public class LandingThemeController : MonoBehaviour
    {
        private static readonly Color GreenCameraBg = new Color(0.2784314f, 0.3882353f, 0.3764706f, 1f);
        private static readonly Color DarkCameraBg = new Color(0.04f, 0.06f, 0.05f, 1f);
        private static readonly Color DarkOverlay = new Color(0.03f, 0.05f, 0.04f, 1f);

        [SerializeField] private float revealDuration = 1.5f;
        [SerializeField] private float holdDarkDuration = 0.35f;

        private Image darkOverlayImage;

        private void Start()
        {
            StartCoroutine(PlayLandingIntro());
        }

        private IEnumerator PlayLandingIntro()
        {
            Camera cam = Camera.main;
            if (cam)
            {
                cam.backgroundColor = DarkCameraBg;
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            GameObject bottomGui = GameObject.Find("BottomMapGui");
            if (bottomGui) bottomGui.SetActive(false);

            Transform logo = GameObject.Find("Logo")?.transform;
            if (logo)
            {
                RotatingLogo rotator = logo.GetComponent<RotatingLogo>();
                if (rotator) rotator.StopRotation();
                logo.localRotation = Quaternion.identity;
            }

            Image background = GameObject.Find("Background")?.GetComponent<Image>();
            if (background)
            {
                background.color = Color.white;
            }

            CreateDarkOverlay();
            yield return new WaitForSeconds(holdDarkDuration);

            if (darkOverlayImage)
            {
                SimpleTween.Value(darkOverlayImage.gameObject, 1f, 0f, revealDuration)
                    .SetEase(EaseAnim.EaseOutQuad)
                    .SetOnUpdate(a =>
                    {
                        Color c = DarkOverlay;
                        c.a = a;
                        darkOverlayImage.color = c;
                    });
            }

            if (cam)
            {
                SimpleTween.Value(gameObject, 0f, 1f, revealDuration)
                    .SetEase(EaseAnim.EaseOutQuad)
                    .SetOnUpdate(t =>
                    {
                        cam.backgroundColor = Color.Lerp(DarkCameraBg, GreenCameraBg, t);
                    });
            }

            yield return new WaitForSeconds(revealDuration);

            if (bottomGui) bottomGui.SetActive(true);

            if (darkOverlayImage)
            {
                Destroy(darkOverlayImage.gameObject);
                darkOverlayImage = null;
            }
        }

        private void CreateDarkOverlay()
        {
            Transform canvas = transform;
            if (canvas.Find("LandingDarkOverlay")) return;

            GameObject overlayGo = new GameObject("LandingDarkOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(canvas, false);
            overlayGo.transform.SetAsLastSibling();

            RectTransform rt = overlayGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            darkOverlayImage = overlayGo.AddComponent<Image>();
            darkOverlayImage.color = DarkOverlay;
            darkOverlayImage.raycastTarget = false;

            Transform logo = GameObject.Find("Logo")?.transform;
            if (logo) logo.SetAsLastSibling();
        }
    }
}
