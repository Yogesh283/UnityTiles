using System.Collections;
using Mkey;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Removes the green GuiMask glow and uses a neutral dark dimmer behind tournament popups.
    /// </summary>
    public static class TournamentPopupBackdrop
    {
        private const int DialogSortOrder = 9500;

        public static void Apply(WarningMessController popup)
        {
            if (!popup)
                return;

            GuiFader_v2 fader = popup.GetComponent<GuiFader_v2>();
            if (!fader)
                return;

            BringToFront(popup);
            StretchFullScreen(fader.GetComponent<RectTransform>());
            ApplyDarkBackground(fader);
            HideGuiMaskGraphic(fader);
            CenterDialogPanel(fader);
        }

        public static void AttachKeeper(WarningMessController popup)
        {
            if (!popup)
                return;

            Apply(popup);
            if (!popup.GetComponent<TournamentPopupBackdropKeeper>())
                popup.gameObject.AddComponent<TournamentPopupBackdropKeeper>();
        }

        private static void BringToFront(WarningMessController popup)
        {
            Canvas canvas = popup.GetComponentInParent<Canvas>();
            if (canvas)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, DialogSortOrder);
            }

            popup.transform.SetAsLastSibling();
        }

        private static void HideGuiMaskGraphic(GuiFader_v2 fader)
        {
            if (!fader.guiMask)
                return;

            // Do NOT deactivate GuiMask — guiPanel is parented under it in Message.prefab.
            Image maskImage = fader.guiMask.GetComponent<Image>();
            if (maskImage)
            {
                maskImage.enabled = false;
                maskImage.color = Color.clear;
                maskImage.raycastTarget = false;
            }

            Mask mask = fader.guiMask.GetComponent<Mask>();
            if (mask)
                mask.enabled = false;
        }

        private static void CenterDialogPanel(GuiFader_v2 fader)
        {
            RectTransform panel = fader.guiPanel;
            RectTransform root = fader.GetComponent<RectTransform>();
            if (!panel || !root)
                return;

            panel.SetParent(root, false);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.localRotation = Quaternion.identity;
            panel.localScale = Vector3.one;

            float width = Mathf.Max(panel.sizeDelta.x, 620f);
            float height = Mathf.Max(panel.sizeDelta.y, 360f);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            panel.SetAsLastSibling();
        }

        private static void ApplyDarkBackground(GuiFader_v2 fader)
        {
            if (fader.backGround is not RectTransform background)
                return;

            StretchFullScreen(background);

            Image bgImage = background.GetComponent<Image>();
            if (!bgImage)
                return;

            bgImage.enabled = true;
            bgImage.sprite = null;
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);
            bgImage.raycastTarget = true;
        }

        private static void StretchFullScreen(RectTransform rt)
        {
            if (!rt)
                return;

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
    }

    internal sealed class TournamentPopupBackdropKeeper : MonoBehaviour
    {
        private void OnEnable() => StartCoroutine(ApplyRoutine());

        private IEnumerator ApplyRoutine()
        {
            WarningMessController popup = GetComponent<WarningMessController>();
            for (int i = 0; i < 6; i++)
            {
                TournamentPopupBackdrop.Apply(popup);
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);
            TournamentPopupBackdrop.Apply(popup);
            Destroy(this);
        }
    }
}
