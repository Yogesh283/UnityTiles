using System;
using System.Collections;
using Mkey;
using Mkey.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Premium VS intro: player cards, glow pulse, 3-2-1-GO countdown.
    /// </summary>
    public class TournamentVsIntroView : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private Text countdownText;
        private Text vsText;
        private Image glowImage;
        private TournamentPremiumWaitingRoomView.PlayerCardView leftCard;
        private TournamentPremiumWaitingRoomView.PlayerCardView rightCard;
        private bool showing;

        public bool IsShowing => showing;

        public static TournamentVsIntroView Create(Transform parent)
        {
            GameObject host = new GameObject("TournamentVsIntro", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            RectTransform rt = host.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            TournamentVsIntroView view = host.AddComponent<TournamentVsIntroView>();
            view.BuildUi();
            host.SetActive(false);
            return view;
        }

        public IEnumerator PlayRoutine(
            RoomPlayerDto localPlayer,
            RoomPlayerDto opponentPlayer,
            Action onComplete)
        {
            showing = true;
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;

            leftCard.Bind(localPlayer, true, localPlayer != null);
            rightCard.Bind(opponentPlayer, false, opponentPlayer != null);

            float fade = 0f;
            while (fade < 1f)
            {
                fade += Time.unscaledDeltaTime * 3f;
                canvasGroup.alpha = Mathf.Clamp01(fade);
                PulseGlow();
                yield return null;
            }

            for (int i = 3; i >= 1; i--)
            {
                countdownText.text = i.ToString();
                countdownText.fontSize = 120;
                PlayTickSound();
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.unscaledDeltaTime;
                    PulseGlow();
                    yield return null;
                }
            }

            countdownText.text = "START!";
            countdownText.fontSize = 88;
            PlayGoSound();

            yield return new WaitForSecondsRealtime(0.45f);
            Hide();
            onComplete?.Invoke();
        }

        public void Hide()
        {
            showing = false;
            gameObject.SetActive(false);
        }

        private void PulseGlow()
        {
            if (!glowImage) return;
            float pulse = 0.35f + Mathf.PingPong(Time.unscaledTime * 2.5f, 0.35f);
            Color c = glowImage.color;
            c.a = pulse;
            glowImage.color = c;
        }

        private void BuildUi()
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

            Image backdrop = CreateImage(transform, "Backdrop", new Color(0f, 0f, 0f, 0.92f));
            Stretch(backdrop.rectTransform);

            glowImage = CreateImage(transform, "Glow", new Color(1f, 0.82f, 0.2f, 0.25f));
            Stretch(glowImage.rectTransform);

            GameObject row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(transform, false);
            RectTransform rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.05f, 0.28f);
            rowRt.anchorMax = new Vector2(0.95f, 0.72f);
            rowRt.offsetMin = Vector2.zero;
            rowRt.offsetMax = Vector2.zero;

            leftCard = TournamentPremiumWaitingRoomView.CreatePlayerCard(row.transform, "Left");
            RectTransform leftRt = leftCard.Root;
            leftRt.anchorMin = new Vector2(0f, 0f);
            leftRt.anchorMax = new Vector2(0.38f, 1f);
            leftRt.offsetMin = Vector2.zero;
            leftRt.offsetMax = Vector2.zero;

            GameObject vsHost = new GameObject("Vs", typeof(RectTransform));
            vsHost.transform.SetParent(row.transform, false);
            RectTransform vsRt = vsHost.GetComponent<RectTransform>();
            vsRt.anchorMin = new Vector2(0.38f, 0f);
            vsRt.anchorMax = new Vector2(0.62f, 1f);
            vsRt.offsetMin = Vector2.zero;
            vsRt.offsetMax = Vector2.zero;

            vsText = CreateText(vsHost.transform, "VS", 72, FontStyle.Bold, new Color(1f, 0.85f, 0.35f));
            Stretch(vsText.rectTransform);

            rightCard = TournamentPremiumWaitingRoomView.CreatePlayerCard(row.transform, "Right");
            RectTransform rightRt = rightCard.Root;
            rightRt.anchorMin = new Vector2(0.62f, 0f);
            rightRt.anchorMax = new Vector2(1f, 1f);
            rightRt.offsetMin = Vector2.zero;
            rightRt.offsetMax = Vector2.zero;

            countdownText = CreateText(transform, string.Empty, 120, FontStyle.Bold, Color.white);
            RectTransform cdRt = countdownText.rectTransform;
            cdRt.anchorMin = new Vector2(0.1f, 0.08f);
            cdRt.anchorMax = new Vector2(0.9f, 0.22f);
            cdRt.offsetMin = Vector2.zero;
            cdRt.offsetMax = Vector2.zero;
        }

        private static void PlayTickSound()
        {
            if (SoundMaster.Instance)
                SoundMaster.Instance.SoundPlayClick(0.15f, null);
        }

        private static void PlayGoSound()
        {
            if (SoundMaster.Instance)
                SoundMaster.Instance.SoundPlayClick(0.3f, null);
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string text, int size, FontStyle style, Color color)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
