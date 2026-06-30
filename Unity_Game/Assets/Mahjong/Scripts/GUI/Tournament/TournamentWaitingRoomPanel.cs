using System;
using System.Collections;
using System.Text;
using Mkey;
using Mkey.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Tournament matchmaking status via the standard app Message popup (same as map/game screens).
    /// </summary>
    public class TournamentWaitingRoomPanel : MonoBehaviour
    {
        private WarningMessController messagePrefab;
        private WarningMessController activePopup;
        private TournamentDefinition tournament;
        private int currentPlayers;
        private float timeLeft;
        private Action onComplete;
        private bool launchStarted;
        private float searchPulse;
        private bool isVisible;

        public bool IsShowing => isVisible && activePopup;

        public void Show(TournamentDefinition data, Action completeCallback)
        {
            if (TournamentJoinDebug.IsFirstJoin(data))
                TournamentJoinDebug.Log("WaitingRoom.Show — standard Message popup");

            tournament = data;
            onComplete = completeCallback;
            launchStarted = false;
            searchPulse = 0f;

            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(data.id);
            currentPlayers = snap.hasRoom ? snap.currentPlayers : 1;
            timeLeft = snap.hasRoom ? snap.countdownSeconds : data.waitingSeconds;

            StopAllCoroutines();
            TournamentApiBridge.RoomUpdated -= OnRoomUpdated;
            TournamentApiBridge.RoomUpdated += OnRoomUpdated;

            OpenStandardPopup();
            StartCoroutine(WaitingRoutine());
        }

        public void Hide()
        {
            TournamentApiBridge.RoomUpdated -= OnRoomUpdated;
            StopAllCoroutines();
            CloseStandardPopup();
            isVisible = false;
        }

        public static TournamentWaitingRoomPanel Create(Transform parent)
        {
            GameObject host = new GameObject("TournamentWaitingRoom");
            host.transform.SetParent(parent, false);
            TournamentWaitingRoomPanel view = host.AddComponent<TournamentWaitingRoomPanel>();
            view.Initialize();
            return view;
        }

        private void Initialize()
        {
            messagePrefab = Resources.Load<WarningMessController>("PopUps/Message");
            if (!messagePrefab)
                Debug.LogError("[TournamentWaitingRoom] Missing Resources/PopUps/Message.prefab");
        }

        private void OnRoomUpdated()
        {
            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
            if (snap.hasRoom)
            {
                currentPlayers = snap.currentPlayers;
                timeLeft = snap.countdownSeconds;
            }

            RefreshPopupText();
        }

        private void OpenStandardPopup()
        {
            CloseStandardPopup();

            GuiController gui = EnsureGuiController();
            if (!gui || !messagePrefab)
                return;

            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
            BuildCaptionAndBody(snap, out string caption, out string body);

            activePopup = gui.ShowMessageWithYesNoCloseButton(
                messagePrefab,
                caption,
                body,
                null,
                null,
                null);

            if (activePopup)
            {
                activePopup.SetMessage(caption, body, false, false, false);
                isVisible = true;
            }
        }

        private void RefreshPopupText()
        {
            if (!activePopup || tournament == null)
                return;

            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
            BuildCaptionAndBody(snap, out string caption, out string body);
            activePopup.Caption = caption;
            activePopup.Message = body;
        }

        private void CloseStandardPopup()
        {
            if (!activePopup)
                return;

            activePopup.CloseWindow();
            activePopup = null;
        }

        private IEnumerator WaitingRoutine()
        {
            float fallbackPoll = 0f;

            while (!launchStarted)
            {
                if (TournamentApiBridge.IsOnlineMode)
                {
                    fallbackPoll += Time.deltaTime;
                    if (fallbackPoll >= 2f)
                    {
                        fallbackPoll = 0f;
                        yield return RefreshApiRoomCoroutine();
                    }
                }

                TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
                if (snap.hasRoom)
                {
                    currentPlayers = snap.currentPlayers;
                    timeLeft = snap.countdownSeconds;
                }

                RefreshPopupText();

                if (snap.status == "starting" || snap.status == "active")
                {
                    launchStarted = true;
                    yield return RunMatchStartSequence(snap);
                    yield break;
                }

                if (!TournamentApiBridge.IsOnlineMode &&
                    Time.realtimeSinceStartup >= tournament.waitingSeconds + 5f)
                {
                    TournamentRoomRegistry.ForcePrepareForLaunch();
                    launchStarted = true;
                    RefreshPopupText();
                    yield return new WaitForSeconds(0.8f);
                    onComplete?.Invoke();
                    Hide();
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator RunMatchStartSequence(TournamentRoomSnapshot snap)
        {
            if (TournamentApiBridge.CurrentRoom?.serverNowMs > 0)
                TournamentServerClock.SyncServerTime(TournamentApiBridge.CurrentRoom.serverNowMs);

            if (snap.matchStartAtMs > 0)
                TournamentServerClock.ScheduleServerStart(snap.matchStartAtMs);
            else if (TournamentApiBridge.CurrentRoom?.matchStartAtMs > 0)
                TournamentServerClock.ScheduleServerStart(TournamentApiBridge.CurrentRoom.matchStartAtMs);

            while (!TournamentServerClock.IsStartTimeReached())
            {
                RefreshPopupText();
                yield return null;
            }

            RefreshPopupText();
            yield return new WaitForSeconds(0.3f);
            onComplete?.Invoke();
            Hide();
        }

        private IEnumerator RefreshApiRoomCoroutine()
        {
            var task = TournamentApiBridge.RefreshActiveRoomAsync();
            while (!task.IsCompleted)
                yield return null;
        }

        private void BuildCaptionAndBody(TournamentRoomSnapshot snap, out string caption, out string body)
        {
            searchPulse += Time.deltaTime;
            int dots = 1 + (Mathf.FloorToInt(searchPulse * 2f) % 3);

            if (tournament == null)
            {
                caption = "Tournament";
                body = string.Empty;
                return;
            }

            currentPlayers = snap.hasRoom ? snap.currentPlayers : currentPlayers;
            string phase = string.IsNullOrEmpty(snap.searchStatus) ? "searching" : snap.searchStatus;

            if (snap.status == "starting" || phase == "match_found")
                caption = "Match Found!";
            else if (phase == "player_joined" || phase == "players_connected")
                caption = "Player Found!";
            else
                caption = "Searching...";

            var sb = new StringBuilder();
            sb.AppendLine($"{tournament.icon} {tournament.displayName}");
            sb.AppendLine();

            if (phase == "players_connected" || phase == "match_found" || phase == "starting")
                sb.AppendLine(TournamentPlayerSearchPresenter.PlayersConnectedLine(currentPlayers, tournament.maxPlayers));
            else
                sb.AppendLine(TournamentPlayerSearchPresenter.PlayersFoundLine(currentPlayers, tournament.maxPlayers));

            sb.AppendLine();

            if (snap.status == "starting" || phase == "match_found")
            {
                int countdown = TournamentServerClock.DisplayCountdownSeconds();
                sb.AppendLine(countdown > 0 ? countdown.ToString() : "GO!");
                sb.AppendLine();
                sb.AppendLine(countdown > 0 ? "Starting in..." : "Game Starting!");
            }
            else
            {
                float displayTime = snap.hasRoom ? snap.countdownSeconds : timeLeft;
                int minutes = Mathf.FloorToInt(displayTime / 60f);
                int seconds = Mathf.FloorToInt(displayTime % 60f);
                sb.AppendLine($"{minutes:00}:{seconds:00}");
                sb.AppendLine();
                sb.AppendLine(TournamentPlayerSearchPresenter.StatusForPhase(
                    currentPlayers <= 1 ? "searching" : phase,
                    dots));
            }

            body = sb.ToString().TrimEnd();
        }

        private static GuiController EnsureGuiController()
        {
            if (GuiController.Instance)
                return GuiController.Instance;

            GuiController existing = FindFirstObjectByType<GuiController>();
            if (existing)
                return existing;

            GameObject go = new GameObject(
                "GuiController",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(GuiController));

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4500;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.4f;

            DontDestroyOnLoad(go);
            return go.GetComponent<GuiController>();
        }
    }
}
