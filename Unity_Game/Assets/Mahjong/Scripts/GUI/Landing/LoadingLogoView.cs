using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Scene load popup: green theme background + static logo.
    /// </summary>
    public class LoadingLogoView : MonoBehaviour
    {
        private static readonly Color GreenBg = new Color(0.2784314f, 0.3882353f, 0.3764706f, 1f);

        [SerializeField] private Vector2 logoSize = new Vector2(420f, 630f);
        [SerializeField] private float logoYOffset = 120f;

        private GameObject logoObject;

        private void OnEnable()
        {
            BuildLoadingLogo();
        }

        private void OnDisable()
        {
            if (logoObject)
            {
                Destroy(logoObject);
                logoObject = null;
            }
        }

        private void BuildLoadingLogo()
        {
            Image bkg = transform.Find("Bkg")?.GetComponent<Image>();
            if (bkg)
            {
                Sprite mapBg = Resources.Load<Sprite>("Landing/MapBackground");
                if (mapBg)
                {
                    bkg.sprite = mapBg;
                    bkg.color = Color.white;
                    bkg.type = Image.Type.Simple;
                    bkg.preserveAspect = false;
                }
                else
                {
                    bkg.sprite = null;
                    bkg.color = GreenBg;
                }
            }

            Transform mask = transform.Find("GuiMask");
            if (!mask) return;

            Image maskImage = mask.GetComponent<Image>();
            if (maskImage)
            {
                maskImage.color = new Color(1f, 1f, 1f, 0f);
            }

            if (mask.Find("LoadingLogo")) return;

            Sprite logoSprite = Resources.Load<Sprite>("Landing/AppLogo");
            if (!logoSprite) return;

            logoObject = new GameObject("LoadingLogo", typeof(RectTransform));
            logoObject.transform.SetParent(mask, false);

            RectTransform rt = logoObject.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = logoSize;
            rt.anchoredPosition = new Vector2(0f, logoYOffset);

            Image logoImage = logoObject.AddComponent<Image>();
            logoImage.sprite = logoSprite;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            logoObject.transform.localRotation = Quaternion.identity;
        }
    }
}
