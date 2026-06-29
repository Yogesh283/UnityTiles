using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    public class TournamentTimerHud : MonoBehaviour
    {
        private Text timerText;
        private Text labelText;

        public static TournamentTimerHud Create()
        {
            Canvas canvas = ResolveCanvas();
            if (!canvas) return null;

            GameObject root = new GameObject("TournamentTimerHud", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            TournamentTimerHud hud = root.AddComponent<TournamentTimerHud>();
            hud.Build(root.GetComponent<RectTransform>());
            return hud;
        }

        private void Build(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            rt.sizeDelta = new Vector2(260f, 72f);

            Image bg = TournamentUIFactory.CreateSlicedImage(rt, "Bg", new Color(0.03f, 0.1f, 0.07f, 0.92f), TournamentSpriteFactory.CardBackground, false);
            TournamentUIFactory.StretchRect(bg.rectTransform);

            labelText = TournamentUIFactory.CreateText(rt, "Label", "TOURNAMENT TIME", 16, FontStyle.Bold, TournamentPremiumTheme.GoldLabel, TextAnchor.UpperCenter);
            RectTransform labelRt = labelText.rectTransform;
            labelRt.anchorMin = new Vector2(0.05f, 0.52f);
            labelRt.anchorMax = new Vector2(0.95f, 0.92f);
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

            timerText = TournamentUIFactory.CreateText(rt, "Timer", "00:00", 34, FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.MiddleCenter);
            RectTransform timerRt = timerText.rectTransform;
            timerRt.anchorMin = new Vector2(0.05f, 0.08f);
            timerRt.anchorMax = new Vector2(0.95f, 0.55f);
            timerRt.offsetMin = timerRt.offsetMax = Vector2.zero;

            if (TournamentMatchManager.IsDuelMode)
            {
                rt.sizeDelta = new Vector2(320f, 96f);
                TournamentMatchParticipant opponent = TournamentMatchManager.GetDuelOpponentForHud();
                if (opponent != null && !string.IsNullOrEmpty(opponent.id))
                {
                    labelText.text = "VS " + TournamentRoom.FormatShortId(opponent.id);
                }
            }
        }

        private void Update()
        {
            if (!timerText || !TournamentSession.GameplayRunning) return;
            timerText.text = FormatTime(TournamentSession.GetLiveElapsedSeconds());
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }

        public void Hide()
        {
            if (gameObject) Destroy(gameObject);
        }

        private static Canvas ResolveCanvas()
        {
            GameObject canvasOver = GameObject.Find("CanvasOver(for popups)");
            if (canvasOver && canvasOver.TryGetComponent(out Canvas overCanvas))
                return overCanvas;

            GameObject canvasMain = GameObject.Find("CanvasMain");
            if (canvasMain && canvasMain.TryGetComponent(out Canvas mainCanvas))
                return mainCanvas;

            return Object.FindFirstObjectByType<Canvas>();
        }
    }
}
