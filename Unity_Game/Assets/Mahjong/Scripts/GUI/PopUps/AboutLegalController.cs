using Mkey.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Match IQ branding + legal links for the About popup.
    /// </summary>
    public class AboutLegalController : MonoBehaviour
    {
        private static readonly Vector2 LogoSize = new Vector2(280f, 420f);

        private void Awake()
        {
            ApplyMatchIqLogo();

            string root = ApiConfig.Current.ServerRoot;
            string privacyUrl = root + "/legal/privacy.html";
            string termsUrl = root + "/legal/terms.html";

            foreach (OpenURLButton link in GetComponentsInChildren<OpenURLButton>(true))
            {
                if (link.gameObject.name == "ButtonPolicy")
                    link.SetUrl(privacyUrl);
                else if (link.gameObject.name == "ButtonTerms")
                    link.SetUrl(termsUrl);
            }

            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.name == "ButtonPolicy" || button.name == "ButtonTerms" || button.name == "ButtonClose")
                    button.interactable = true;
            }
        }

        private void ApplyMatchIqLogo()
        {
            Transform logoTransform = transform.Find("GuiMask/Panel/Image");
            if (!logoTransform) return;

            Sprite matchIqLogo = Resources.Load<Sprite>("Landing/AppLogo");
            if (!matchIqLogo) return;

            Image logoImage = logoTransform.GetComponent<Image>();
            if (!logoImage) return;

            logoImage.sprite = matchIqLogo;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;

            RectTransform rt = logoTransform as RectTransform;
            if (rt)
                rt.sizeDelta = LogoSize;
        }
    }
}
