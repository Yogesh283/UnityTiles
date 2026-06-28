using System.Text;
using Mkey;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    public class TournamentResultPanel : MonoBehaviour
    {
        private static TournamentResultPanel instance;

        public static void Show(TournamentMatchResult result)
        {
            if (result == null) return;

            if (!instance)
            {
                GameObject host = new GameObject("TournamentResultPanel");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<TournamentResultPanel>();
                instance.BuildUi(host.transform);
            }

            instance.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();
            instance.Populate(result);
        }

        private Text headerText;
        private Text statsText;
        private Text prizeText;
        private Text leaderboardText;
        private RectTransform panel;

        private void BuildUi(Transform root)
        {
            Canvas canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000;
            root.gameObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform overlay = TournamentUIFactory.CreateRect(root, "Overlay");
            TournamentUIFactory.StretchRect(overlay);
            Image dim = TournamentUIFactory.CreateImage(overlay, "Dim", new Color(0f, 0f, 0f, 0.82f));
            TournamentUIFactory.StretchRect(dim.rectTransform);
            dim.raycastTarget = true;

            panel = TournamentUIFactory.CreateRect(overlay, "Panel");
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(920f, 1180f);
            TournamentPremiumUI.CreateDialogPanel(panel);

            headerText = TournamentUIFactory.CreateText(panel, "Header", "TOURNAMENT RESULT", 38, FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.UpperCenter);
            headerText.rectTransform.anchorMin = new Vector2(0.05f, 0.88f);
            headerText.rectTransform.anchorMax = new Vector2(0.95f, 0.97f);
            headerText.rectTransform.offsetMin = headerText.rectTransform.offsetMax = Vector2.zero;
            TournamentUIFactory.AddGoldOutline(headerText);

            statsText = TournamentUIFactory.CreateText(panel, "Stats", string.Empty, 24, FontStyle.Bold, TournamentPremiumTheme.TextWhite, TextAnchor.UpperLeft);
            statsText.rectTransform.anchorMin = new Vector2(0.08f, 0.52f);
            statsText.rectTransform.anchorMax = new Vector2(0.92f, 0.86f);
            statsText.rectTransform.offsetMin = statsText.rectTransform.offsetMax = Vector2.zero;

            prizeText = TournamentUIFactory.CreateText(panel, "Prize", string.Empty, 30, FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.MiddleCenter);
            prizeText.rectTransform.anchorMin = new Vector2(0.08f, 0.42f);
            prizeText.rectTransform.anchorMax = new Vector2(0.92f, 0.5f);
            prizeText.rectTransform.offsetMin = prizeText.rectTransform.offsetMax = Vector2.zero;

            Text boardLabel = TournamentUIFactory.CreateText(panel, "BoardLabel", "LEADERBOARD", 20, FontStyle.Bold, TournamentPremiumTheme.GoldLabel, TextAnchor.UpperLeft);
            boardLabel.rectTransform.anchorMin = new Vector2(0.08f, 0.36f);
            boardLabel.rectTransform.anchorMax = new Vector2(0.92f, 0.4f);
            boardLabel.rectTransform.offsetMin = boardLabel.rectTransform.offsetMax = Vector2.zero;

            leaderboardText = TournamentUIFactory.CreateText(panel, "Leaderboard", string.Empty, 18, FontStyle.Normal, TournamentPremiumTheme.TextSoft, TextAnchor.UpperLeft);
            leaderboardText.rectTransform.anchorMin = new Vector2(0.08f, 0.14f);
            leaderboardText.rectTransform.anchorMax = new Vector2(0.92f, 0.35f);
            leaderboardText.rectTransform.offsetMin = leaderboardText.rectTransform.offsetMax = Vector2.zero;

            Button continueBtn = CreateContinueButton(panel);
            continueBtn.onClick.AddListener(OnContinueClicked);

            gameObject.SetActive(false);
        }

        private static Button CreateContinueButton(RectTransform parent)
        {
            RectTransform rt = TournamentUIFactory.CreateRect(parent, "ContinueButton");
            rt.anchorMin = new Vector2(0.15f, 0.03f);
            rt.anchorMax = new Vector2(0.85f, 0.11f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            Image bg = TournamentUIFactory.CreateSlicedImage(rt, "Bg", Color.white, TournamentSpriteFactory.ButtonGreen, true);
            TournamentUIFactory.StretchRect(bg.rectTransform);

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            Text label = TournamentUIFactory.CreateText(rt, "Label", "CONTINUE", 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            TournamentUIFactory.StretchRect(label.rectTransform);
            return button;
        }

        private void Populate(TournamentMatchResult result)
        {
            headerText.text = $"{result.tournamentName}\nTOURNAMENT RESULT";

            statsText.text =
                $"Your Rank: #{result.playerRank} / {result.maxPlayers:N0}\n\n" +
                $"Time: {FormatTime(result.playerTimeSeconds)}\n" +
                $"Score: {result.playerScore:N0}\n" +
                $"Moves: {result.playerMoves:N0}";

            if (result.prizeWon > 0)
            {
                prizeText.text = $"+{result.prizeWon:N0} Coins Won!";
                LevelCoinRewardEffect.Play(result.prizeWon);
            }
            else
            {
                prizeText.text = "No prize this time — keep practicing!";
            }

            StringBuilder sb = new StringBuilder();
            if (result.leaderboard != null)
            {
                foreach (TournamentParticipantResult row in result.leaderboard)
                {
                    string marker = row.isPlayer ? " ★" : string.Empty;
                    sb.AppendLine(
                        $"#{row.rank,2}  {row.name,-14}  {row.score,6:N0} pts  {FormatTime(row.timeSeconds)}  {row.moves,3} mv{marker}");
                }
            }

            leaderboardText.text = sb.ToString().TrimEnd();

            if (panel)
            {
                panel.localScale = Vector3.one * 0.92f;
                SimpleTween.Value(gameObject, 0f, 1f, 0.25f)
                    .SetEase(EaseAnim.EaseOutBack)
                    .SetOnUpdate(t => panel.localScale = Vector3.LerpUnclamped(Vector3.one * 0.92f, Vector3.one, t));
            }
        }

        private static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }

        private void OnContinueClicked()
        {
            TournamentSession.Clear();
            gameObject.SetActive(false);

            if (SceneLoader.Instance)
                SceneLoader.Instance.LoadScene(TournamentSession.TournamentSceneIndex);
            else
                SceneManager.LoadScene(TournamentSession.TournamentSceneIndex);
        }
    }
}
