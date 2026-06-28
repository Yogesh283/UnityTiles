using System;
using System.Collections;
using Mkey.Network;
using Mkey;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    public class TournamentWaitingRoomPanel : MonoBehaviour
    {
        private Text titleText;
        private Text playersText;
        private Text countdownText;
        private Text statusText;
        private TournamentDefinition tournament;
        private int currentPlayers;
        private float timeLeft;
        private Action onComplete;

        public void Show(TournamentDefinition data, Action completeCallback)
        {
            if (TournamentJoinDebug.IsFirstJoin(data))
                TournamentJoinDebug.Log("WaitingRoom.Show executing — setting panel active");

            tournament = data;
            onComplete = completeCallback;
            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(data.id);
            currentPlayers = snap.hasRoom ? snap.currentPlayers : 1;
            timeLeft = snap.hasRoom ? snap.countdownSeconds : data.waitingSeconds;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            UpdateLabels();
            StopAllCoroutines();
            StartCoroutine(WaitingRoutine());
            PlayIntro();
        }

        public void Hide()
        {
            SimpleTween.ForceCancel(gameObject);
            gameObject.SetActive(false);
            StopAllCoroutines();
        }

        public static TournamentWaitingRoomPanel Create(Transform parent)
        {
            RectTransform overlay = TournamentUIFactory.CreateRect(parent, "WaitingRoom");
            TournamentUIFactory.StretchRect(overlay);
            Image dim = TournamentUIFactory.CreateImage(overlay, "Dim", new Color(0f, 0f, 0f, 0.72f));
            TournamentUIFactory.StretchRect(dim.rectTransform);
            dim.raycastTarget = true;

            RectTransform panel = TournamentUIFactory.CreateRect(overlay, "Panel");
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(TournamentLayoutMetrics.S(820f), TournamentLayoutMetrics.S(580f));

            TournamentPremiumUI.CreateDialogPanel(panel);

            Text header = TournamentUIFactory.CreateText(panel, "Header", "WAITING ROOM", TournamentLayoutMetrics.Font(38f), FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.UpperCenter);
            header.rectTransform.anchorMin = new Vector2(0f, 0.78f);
            header.rectTransform.anchorMax = new Vector2(1f, 0.95f);
            TournamentUIFactory.AddGoldOutline(header);

            Text tournamentName = TournamentUIFactory.CreateText(panel, "TournamentName", "", TournamentLayoutMetrics.Font(28f), FontStyle.Bold, TournamentPremiumTheme.TextSoft, TextAnchor.UpperCenter);
            tournamentName.rectTransform.anchorMin = new Vector2(0f, 0.66f);
            tournamentName.rectTransform.anchorMax = new Vector2(1f, 0.78f);

            Text players = TournamentUIFactory.CreateText(panel, "Players", "", TournamentLayoutMetrics.Font(30f), FontStyle.Bold, TournamentPremiumTheme.TextWhite, TextAnchor.MiddleCenter);
            players.rectTransform.anchorMin = new Vector2(0.1f, 0.48f);
            players.rectTransform.anchorMax = new Vector2(0.9f, 0.62f);

            Text countdown = TournamentUIFactory.CreateText(panel, "Countdown", "", TournamentLayoutMetrics.Font(50f), FontStyle.Bold, TournamentPremiumTheme.GoldBright, TextAnchor.MiddleCenter);
            countdown.rectTransform.anchorMin = new Vector2(0.1f, 0.28f);
            countdown.rectTransform.anchorMax = new Vector2(0.9f, 0.46f);

            Text status = TournamentUIFactory.CreateText(panel, "Status", "Finding players...", 26, FontStyle.Italic, TournamentPremiumTheme.TextMuted, TextAnchor.LowerCenter);
            status.rectTransform.anchorMin = new Vector2(0.1f, 0.08f);
            status.rectTransform.anchorMax = new Vector2(0.9f, 0.2f);

            TournamentWaitingRoomPanel view = overlay.gameObject.AddComponent<TournamentWaitingRoomPanel>();
            view.titleText = tournamentName;
            view.playersText = players;
            view.countdownText = countdown;
            view.statusText = status;
            overlay.gameObject.SetActive(false);
            return view;
        }

        private void PlayIntro()
        {
            RectTransform panel = transform.Find("Panel") as RectTransform;
            if (!panel) return;
            panel.localScale = Vector3.one * 0.9f;
            SimpleTween.Value(gameObject, 0f, 1f, 0.22f)
                .SetEase(EaseAnim.EaseOutBack)
                .SetOnUpdate(t => panel.localScale = Vector3.LerpUnclamped(Vector3.one * 0.9f, Vector3.one, t));
        }

        private IEnumerator WaitingRoutine()
        {
            float maxWait = tournament.waitingSeconds + 10f;
            float startedAt = Time.realtimeSinceStartup;
            float apiPollTimer = 0f;

            while (true)
            {
                if (TournamentApiBridge.IsOnlineMode)
                {
                    apiPollTimer += Time.deltaTime;
                    if (apiPollTimer >= 1f)
                    {
                        apiPollTimer = 0f;
                        yield return RefreshApiRoomCoroutine();
                    }
                }

                TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);

                if (snap.hasRoom)
                {
                    currentPlayers = snap.currentPlayers;
                    timeLeft = snap.countdownSeconds;
                }

                UpdateLabels();

                if (snap.shouldLaunch)
                {
                    statusText.text = "Game Starting!";
                    countdownText.text = "GO!";
                    yield return new WaitForSeconds(1.2f);
                    onComplete?.Invoke();
                    Hide();
                    yield break;
                }

                // Anti-freeze: never stay in waiting room forever
                if (Time.realtimeSinceStartup - startedAt >= maxWait)
                {
                    TournamentRoomRegistry.ForcePrepareForLaunch();
                    statusText.text = "Game Starting!";
                    countdownText.text = "GO!";
                    yield return new WaitForSeconds(0.8f);
                    onComplete?.Invoke();
                    Hide();
                    yield break;
                }

                yield return null;
            }
        }

        private void UpdateLabels()
        {
            if (tournament == null) return;
            titleText.text = $"{tournament.icon} {tournament.displayName}";
            playersText.text = $"Waiting Players: {currentPlayers} / {tournament.maxPlayers}";

            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
            float displayTime = snap.hasRoom ? snap.countdownSeconds : timeLeft;
            int minutes = Mathf.FloorToInt(displayTime / 60f);
            int seconds = Mathf.FloorToInt(displayTime % 60f);
            countdownText.text = $"{minutes:00}:{seconds:00}";
            statusText.text = snap.hasRoom ? snap.statusMessage :
                (currentPlayers >= tournament.maxPlayers ? "Lobby full — starting soon" : "Finding players...");
        }

        private IEnumerator RefreshApiRoomCoroutine()
        {
            var task = TournamentApiBridge.RefreshActiveRoomAsync();
            while (!task.IsCompleted)
                yield return null;
        }
    }
}
