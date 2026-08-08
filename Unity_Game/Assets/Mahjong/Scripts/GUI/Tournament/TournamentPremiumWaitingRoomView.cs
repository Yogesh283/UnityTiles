using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mkey;
using Mkey.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Tournament waiting room using the same wooden Message popup as join / result dialogs.
    /// </summary>
    public class TournamentPremiumWaitingRoomView : MonoBehaviour
    {
        // Canvas reference units (1080×1920) — not screen pixels.
        private const float WaitingPanelWidth = 620f;
        private const float WaitingPanelHeight = 1100f;

        private WarningMessController popup;
        private int lastPlayerCount;
        private bool visible;
        private float animatedWaitSeconds;
        private float lastBindRealtime = -1f;

        public bool IsVisible => visible && popup != null;

        public static TournamentPremiumWaitingRoomView Create(Transform parent)
        {
            GameObject host = new GameObject("PremiumWaitingRoom", typeof(RectTransform));
            host.transform.SetParent(parent, false);
            RectTransform rt = host.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            TournamentPremiumWaitingRoomView view = host.AddComponent<TournamentPremiumWaitingRoomView>();
            host.SetActive(false);
            return view;
        }

        private Action onCancelRequested;

        public void Show(Action cancelCallback = null)
        {
            onCancelRequested = cancelCallback;
            gameObject.SetActive(true);

            if (popup != null)
            {
                visible = true;
                ReparentToScreenOverlay(popup);
                ApplyWaitingRoomLayout(popup);
                StartCoroutine(FinalizePopupLayoutNextFrame());
                return;
            }

            EnsurePopup();
            if (!popup)
            {
                Debug.LogError("[WAITING_ROOM_OPEN] popup failed — Resources/PopUps/Message missing or GuiController unavailable");
                return;
            }

            visible = true;
            lastPlayerCount = 0;
            animatedWaitSeconds = 0f;
            lastBindRealtime = Time.realtimeSinceStartup;
            StartCoroutine(FinalizePopupLayoutNextFrame());
            Debug.Log("[WAITING_ROOM_OPEN] premium waiting popup shown on overlay");
        }

        private void OnPopupClosed(PopUpsController controller)
        {
            popup = null;
            visible = false;

            if (controller)
                Destroy(controller.gameObject);

            if (controller is WarningMessController warning &&
                warning.Answer == MessageAnswer.Cancel)
            {
                onCancelRequested?.Invoke();
            }
        }

        public void Hide()
        {
            visible = false;
            animatedWaitSeconds = 0f;
            lastBindRealtime = -1f;
            if (popup)
            {
                Destroy(popup.gameObject);
                popup = null;
            }
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (visible && popup)
                ForcePopupCenter(popup);
        }

        public void Bind(TournamentDefinition tournament, TournamentRoomSnapshot snap, float searchPulse, float clientWaitSeconds = 0f)
        {
            if (tournament == null || !popup)
                return;

            float now = Time.realtimeSinceStartup;
            if (lastBindRealtime < 0f)
                lastBindRealtime = now;
            float bindDelta = Mathf.Max(0f, now - lastBindRealtime);
            lastBindRealtime = now;
            animatedWaitSeconds += bindDelta;

            string phase = string.IsNullOrEmpty(snap.searchStatus) ? "searching" : snap.searchStatus;
            int current = snap.hasRoom ? snap.currentPlayers : 1;
            int max = snap.maxPlayers > 0 ? snap.maxPlayers : tournament.maxPlayers;
            bool duel = max <= 2;
            bool roomFull = current >= max;
            float serverWait = clientWaitSeconds > 0f ? clientWaitSeconds : snap.countdownSeconds;
            float waitDisplay = Mathf.Max(serverWait, animatedWaitSeconds);

            string caption = BuildCaption(tournament, snap, phase, roomFull);
            string message = BuildMessage(tournament, snap, searchPulse, waitDisplay, phase, current, max, duel, roomFull);

            if (popup.caption)
                popup.caption.text = caption;
            if (popup.message)
                popup.message.text = message;
            ApplyWaitingRoomLayout(popup);

            if (current > lastPlayerCount && lastPlayerCount > 0)
                PlayJoinSound();

            lastPlayerCount = current;
        }

        private void EnsurePopup()
        {
            if (popup)
                return;

            WarningMessController prefab = Resources.Load<WarningMessController>("PopUps/Message");
            GuiController gui = EnsureGuiController();
            if (!prefab || !gui)
            {
                Debug.LogError("[Tournament] Waiting room popup missing: Resources/PopUps/Message or GuiController");
                return;
            }

            BringGuiToFront(gui);

            popup = prefab.CreateWindow() as WarningMessController;
            if (!popup)
            {
                Debug.LogError("[WAITING_ROOM_OPEN] popup CreateWindow failed");
                return;
            }

            popup.transform.SetAsLastSibling();
            ReparentToScreenOverlay(popup);

            if (popup.cancelButton)
                popup.cancelButton.gameObject.SetActive(true);
            if (popup.yesButton)
                popup.yesButton.gameObject.SetActive(false);
            if (popup.noButton)
                popup.noButton.gameObject.SetActive(false);

            popup.PopUpInit(null, OnPopupClosed);

            if (popup.caption)
                popup.caption.text = "Searching...";
            if (popup.message)
                popup.message.text = "Connecting to tournament...";

            ApplyWaitingRoomLayout(popup);
        }

        private IEnumerator FinalizePopupLayoutNextFrame()
        {
            yield return null;
            if (!popup)
                yield break;

            ApplyWaitingRoomLayout(popup);
            Canvas.ForceUpdateCanvases();

            // ShowWindow re-activates GuiMask during fade — re-apply after animation.
            yield return new WaitForSeconds(0.25f);
            if (!popup)
                yield break;

            ApplyWaitingRoomLayout(popup);
            Canvas.ForceUpdateCanvases();
        }

        private static void ApplyWaitingRoomLayout(WarningMessController waitingPopup)
        {
            if (!waitingPopup)
                return;

            GuiFader_v2 fader = waitingPopup.GetComponent<GuiFader_v2>();
            Vector2 panelSize = new Vector2(
                TournamentLayoutMetrics.S(WaitingPanelWidth),
                TournamentLayoutMetrics.S(WaitingPanelHeight));

            if (fader?.guiPanel)
            {
                DisableGuiMask(fader);
                CenterWaitingPanel(fader, panelSize);
            }

            ApplyWaitingRoomTextAreas(waitingPopup);
            ApplyWaitingRoomButtons(waitingPopup, fader?.guiPanel);
            ForcePopupCenter(waitingPopup);
        }

        private static void DisableGuiMask(GuiFader_v2 fader)
        {
            if (!fader?.guiMask)
                return;

            fader.guiMask.gameObject.SetActive(false);
            Image maskImage = fader.guiMask.GetComponent<Image>();
            if (maskImage)
                maskImage.enabled = false;
            Mask mask = fader.guiMask.GetComponent<Mask>();
            if (mask)
                mask.enabled = false;
        }

        private static void ReparentToScreenOverlay(WarningMessController waitingPopup)
        {
            if (!waitingPopup)
                return;

            Transform overlay = TournamentGlobalWaitingRoom.OverlayRoot;
            if (!overlay)
                return;

            if (!overlay.gameObject.activeSelf)
                overlay.gameObject.SetActive(true);

            waitingPopup.transform.SetParent(overlay, false);
            StretchFullScreen(waitingPopup.GetComponent<RectTransform>());
            waitingPopup.transform.SetAsLastSibling();
            waitingPopup.transform.localPosition = Vector3.zero;
            waitingPopup.transform.localRotation = Quaternion.identity;
            waitingPopup.transform.localScale = Vector3.one;
        }

        private static void CenterWaitingPanel(GuiFader_v2 fader, Vector2 panelSize)
        {
            RectTransform root = fader.GetComponent<RectTransform>();
            StretchFullScreen(root);

            if (fader.backGround is RectTransform background)
            {
                StretchFullScreen(background);
                Image bgImage = background.GetComponent<Image>();
                if (bgImage)
                {
                    bgImage.enabled = true;
                    bgImage.color = new Color(0f, 0f, 0f, 0.55f);
                }
            }

            RectTransform panel = fader.guiPanel;
            if (!panel)
                return;

            panel.SetParent(root, false);
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            Vector2 size = ResolvePanelSize(panelSize);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            panel.anchoredPosition = Vector2.zero;
            panel.localRotation = Quaternion.identity;
            panel.localScale = Vector3.one;
            panel.SetAsLastSibling();
        }

        private static Vector2 ResolvePanelSize(Vector2 desired)
        {
            float width = Mathf.Max(desired.x, WaitingPanelWidth);
            float height = Mathf.Max(desired.y, WaitingPanelHeight);
            width = Mathf.Clamp(width, 520f, TournamentLayoutMetrics.RefWidth * 0.78f);
            height = Mathf.Clamp(height, 900f, TournamentLayoutMetrics.RefHeight * 0.78f);
            return new Vector2(width, height);
        }

        private static void ForcePopupCenter(WarningMessController waitingPopup)
        {
            if (!waitingPopup)
                return;

            GuiFader_v2 fader = waitingPopup.GetComponent<GuiFader_v2>();
            RectTransform root = fader ? fader.GetComponent<RectTransform>() : null;
            RectTransform panel = fader ? fader.guiPanel : null;
            if (!root || !panel)
                return;

            Vector3[] corners = new Vector3[4];
            root.GetWorldCorners(corners);
            Vector3 center = (corners[0] + corners[2]) * 0.5f;

            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.localRotation = Quaternion.identity;
            panel.localScale = Vector3.one;

#if WAITING_ROOM_POS_DEBUG
            LogPositionDebug(root, panel, center);
#endif
        }

#if WAITING_ROOM_POS_DEBUG
        private static void LogPositionDebug(RectTransform root, RectTransform panel, Vector3 targetCenter)
        {
            Vector3[] rootCorners = new Vector3[4];
            Vector3[] panelCorners = new Vector3[4];
            root.GetWorldCorners(rootCorners);
            panel.GetWorldCorners(panelCorners);
            Vector3 rootCenter = (rootCorners[0] + rootCorners[2]) * 0.5f;
            Vector3 panelCenter = (panelCorners[0] + panelCorners[2]) * 0.5f;
            Vector3 delta = panelCenter - rootCenter;

            Debug.Log(
                "[WaitingPopupPos] " +
                $"rootCenter=({rootCenter.x:F1},{rootCenter.y:F1}) " +
                $"targetCenter=({targetCenter.x:F1},{targetCenter.y:F1}) " +
                $"panelCenter=({panelCenter.x:F1},{panelCenter.y:F1}) " +
                $"delta=({delta.x:F1},{delta.y:F1}) " +
                $"panelAnchored=({panel.anchoredPosition.x:F1},{panel.anchoredPosition.y:F1})");
        }
#endif

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

        private static void ApplyWaitingRoomTextAreas(WarningMessController waitingPopup)
        {
            const float sideInset = 0.07f;
            const float captionMinY = 0.78f;
            const float messageMinY = 0.16f;
            const float messageMaxY = 0.77f;

            if (waitingPopup.caption)
            {
                bool hasCaption = !string.IsNullOrWhiteSpace(waitingPopup.caption.text);
                waitingPopup.caption.gameObject.SetActive(hasCaption);
                if (hasCaption)
                {
                    ConfigureWaitingText(
                        waitingPopup.caption,
                        new Vector2(sideInset, captionMinY),
                        new Vector2(1f - sideInset, 0.96f),
                        TournamentLayoutMetrics.Font(34f),
                        TournamentLayoutMetrics.Font(24f),
                        TournamentLayoutMetrics.Font(40f),
                        FontStyle.Bold,
                        TextAnchor.UpperCenter);
                }
            }

            if (waitingPopup.message)
            {
                bool hasMessage = !string.IsNullOrWhiteSpace(waitingPopup.message.text);
                waitingPopup.message.gameObject.SetActive(hasMessage);
                if (hasMessage)
                {
                    ConfigureWaitingText(
                        waitingPopup.message,
                        new Vector2(sideInset, messageMinY),
                        new Vector2(1f - sideInset, messageMaxY),
                        TournamentLayoutMetrics.Font(30f),
                        TournamentLayoutMetrics.Font(18f),
                        TournamentLayoutMetrics.Font(34f),
                        FontStyle.Normal,
                        TextAnchor.UpperCenter);
                    waitingPopup.message.lineSpacing = 0.95f;
                }
            }
        }

        private static void ConfigureWaitingText(
            Text text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int fontSize,
            int minBestFitSize,
            int maxBestFitSize,
            FontStyle style,
            TextAnchor alignment)
        {
            if (!text)
                return;

            text.fontStyle = style;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minBestFitSize;
            text.resizeTextMaxSize = maxBestFitSize;
            text.fontSize = fontSize;

            RectTransform rt = text.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private static void ApplyWaitingRoomButtons(WarningMessController waitingPopup, RectTransform panel)
        {
            if (!waitingPopup || !panel)
                return;

            RectTransform buttonsRoot = FindButtonsRoot(waitingPopup, panel);
            if (buttonsRoot)
                buttonsRoot.gameObject.SetActive(false);

            Button cancel = waitingPopup.cancelButton;
            if (!cancel)
                return;

            cancel.gameObject.SetActive(true);
            RectTransform cancelRt = cancel.GetComponent<RectTransform>();
            if (!cancelRt)
                return;

            if (cancelRt.parent != panel)
                cancelRt.SetParent(panel, false);

            cancelRt.anchorMin = new Vector2(0.18f, 0.04f);
            cancelRt.anchorMax = new Vector2(0.82f, 0.13f);
            cancelRt.offsetMin = Vector2.zero;
            cancelRt.offsetMax = Vector2.zero;
            cancelRt.pivot = new Vector2(0.5f, 0.5f);
            cancelRt.anchoredPosition = Vector2.zero;

            Canvas.ForceUpdateCanvases();
            ApplyCancelButtonTheme(cancel, waitingPopup.yesButton);
            ConfigureCancelLabel(cancel, waitingPopup.yesButton);
        }

        private static void ApplyCancelButtonTheme(Button cancel, Button styleSource)
        {
            if (!cancel)
                return;

            PopupButtonTheme.Apply(cancel.transform);

            Image cancelImage = cancel.GetComponent<Image>();
            if (!cancelImage || !styleSource)
                return;

            Image sourceImage = styleSource.GetComponent<Image>();
            if (!sourceImage || !sourceImage.sprite)
                return;

            cancelImage.sprite = sourceImage.sprite;
            cancelImage.color = Color.white;
            cancelImage.type = Image.Type.Simple;
            cancelImage.preserveAspect = false;
            cancel.transition = Selectable.Transition.SpriteSwap;
            cancel.spriteState = styleSource.spriteState;
        }

        private static void ConfigureCancelLabel(Button cancel, Button styleSource)
        {
            if (!cancel)
                return;

            Text label = EnsureCancelLabel(cancel.transform, styleSource);
            if (!label)
                return;

            label.text = "Cancel";
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontStyle = FontStyle.Bold;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = false;
            label.fontSize = TournamentLayoutMetrics.Font(30f);
            label.raycastTarget = false;

            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = Vector2.zero;
        }

        private static Text EnsureCancelLabel(Transform cancelTransform, Button styleSource)
        {
            Transform existing = cancelTransform.Find("Text");
            if (existing && existing.TryGetComponent(out Text existingLabel))
                return existingLabel;

            Text reference = null;
            if (styleSource)
                reference = styleSource.GetComponentInChildren<Text>(true);

            GameObject labelGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(cancelTransform, false);

            Text label = labelGo.GetComponent<Text>();
            label.font = reference
                ? reference.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return label;
        }

        private static RectTransform FindButtonsRoot(WarningMessController waitingPopup, RectTransform panel)
        {
            if (waitingPopup.yesButton &&
                waitingPopup.yesButton.transform.parent is RectTransform yesParent &&
                yesParent != panel)
                return yesParent;

            Transform buttons = panel.Find("Buttons");
            return buttons ? buttons as RectTransform : null;
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
            canvas.sortingOrder = 9600;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.4f;

            return go.GetComponent<GuiController>();
        }

        private static void BringGuiToFront(GuiController gui)
        {
            Canvas canvas = gui.GetComponent<Canvas>();
            if (!canvas)
                canvas = gui.GetComponentInParent<Canvas>();

            if (canvas)
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 9600);
        }

        private static string BuildCaption(
            TournamentDefinition tournament,
            TournamentRoomSnapshot snap,
            string phase,
            bool roomFull)
        {
            if (snap.status == "starting" || phase == "match_found")
                return $"{tournament.icon} Match Found!";

            if (roomFull)
                return $"{tournament.icon} Player Found!";

            return $"{tournament.icon} {tournament.displayName}";
        }

        private static string BuildMessage(
            TournamentDefinition tournament,
            TournamentRoomSnapshot snap,
            float searchPulse,
            float waitDisplay,
            string phase,
            int current,
            int max,
            bool duel,
            bool roomFull)
        {
            int prize = TournamentPrizeTable.GetPrize(tournament.id, 1);
            var sb = new StringBuilder(256);

            sb.AppendLine($"Entry Fee: {tournament.entryFee:N0} Coins");
            sb.AppendLine($"Winning Prize: {prize:N0} Coins");
            sb.AppendLine();
            sb.AppendLine($"Players: {current} / {max}");

            if (snap.hasRoom && !string.IsNullOrEmpty(snap.roomId))
                sb.AppendLine($"Room: {TournamentRoom.FormatShortId(snap.roomId)}");

            sb.AppendLine();
            sb.Append(BuildPlayerSection(snap, tournament, current, max, duel));
            sb.AppendLine();
            sb.AppendLine(BuildStatusLine(snap, phase, roomFull, duel, searchPulse, waitDisplay));
            sb.Append(ResolveConnectionStatus());
            return sb.ToString().TrimEnd();
        }

        private static string BuildPlayerSection(
            TournamentRoomSnapshot snap,
            TournamentDefinition tournament,
            int current,
            int max,
            bool duel)
        {
            List<RoomPlayerDto> players = BuildPlayerList(snap, tournament, current, max);
            if (duel)
                return BuildDuelPlayerLines(players);

            var sb = new StringBuilder();
            foreach (RoomPlayerDto player in players)
            {
                if (player == null)
                    continue;

                sb.AppendLine($"• {FormatPlayerName(player)}");
            }

            if (current < max)
                sb.AppendLine("• Searching...");

            return sb.ToString().TrimEnd();
        }

        private static string BuildDuelPlayerLines(List<RoomPlayerDto> players)
        {
            RoomPlayerDto local = null;
            RoomPlayerDto opponent = null;
            int localUserId = NetworkManager.HasInstance ? NetworkManager.Instance.UserId : 0;

            foreach (RoomPlayerDto dto in players)
            {
                if (dto == null) continue;
                if (dto.userId == localUserId || (localUserId == 0 && dto.displayName == "You"))
                    local = dto;
                else if (opponent == null)
                    opponent = dto;
            }

            if (local == null && players.Count > 0)
                local = players[0];

            string you = local != null ? FormatPlayerName(local) : "You";
            string them = opponent != null ? FormatPlayerName(opponent) : "Searching...";
            return $"{you}\nvs\n{them}";
        }

        private static string FormatPlayerName(RoomPlayerDto player)
        {
            if (player == null)
                return "Searching...";

            if (!string.IsNullOrEmpty(player.displayName))
                return player.displayName;
            if (!string.IsNullOrEmpty(player.username))
                return player.username;
            return "Player";
        }

        private static string BuildStatusLine(
            TournamentRoomSnapshot snap,
            string phase,
            bool roomFull,
            bool duel,
            float searchPulse,
            float waitDisplay)
        {
            if (snap.status == "starting" || phase == "match_found")
            {
                int cd = TournamentServerClock.DisplayCountdownSeconds();
                return cd > 0 ? $"Starting in {cd}..." : "GO!";
            }

            if (roomFull)
                return $"Preparing match... {FormatWaitTimer(waitDisplay)}";

            if (duel)
            {
                int dots = 1 + (Mathf.FloorToInt(searchPulse * 2f) % 3);
                return TournamentPlayerSearchPresenter.StatusForPhase("searching", dots) +
                       $"  {FormatWaitTimer(waitDisplay)}";
            }

            if (phase == "players_connected" || phase == "player_joined")
                return TournamentPlayerSearchPresenter.StatusForPhase(phase, 0) + $"  {FormatWaitTimer(waitDisplay)}";

            int searchDots = 1 + (Mathf.FloorToInt(searchPulse * 2f) % 3);
            return TournamentPlayerSearchPresenter.StatusForPhase("searching", searchDots) +
                   $"  {FormatWaitTimer(waitDisplay)}";
        }

        private static string FormatWaitTimer(float displaySeconds)
        {
            if (displaySeconds < 0f)
                displaySeconds = 0f;

            int minutes = Mathf.FloorToInt(displaySeconds / 60f);
            int seconds = Mathf.FloorToInt(displaySeconds % 60f);
            return $"({minutes:00}:{seconds:00})";
        }

        private static string ResolveConnectionStatus()
        {
            if (!TournamentApiBridge.IsOnlineMode)
                return "\n● Local Practice";

            if (TournamentApiBridge.IsReconnecting || TournamentRoomWebSocket.IsReconnecting)
                return "\n○ Reconnecting...";

            if (!TournamentApiBridge.HasActiveApiSession)
                return "\n○ Searching for Players...";

            return TournamentRoomWebSocket.IsConnected
                ? "\n● Live — Connected"
                : "\n○ Connecting to server...";
        }

        private static List<RoomPlayerDto> BuildPlayerList(
            TournamentRoomSnapshot snap,
            TournamentDefinition tournament,
            int current,
            int max)
        {
            var list = new List<RoomPlayerDto>();
            if (snap.players != null && snap.players.Count > 0)
            {
                list.AddRange(snap.players);
                return list;
            }

            list.Add(BuildLocalFallbackPlayer());

            if (!TournamentApiBridge.IsOnlineMode && TournamentRoomRegistry.HasLocalRoom)
            {
                TournamentRoom room = TournamentRoomRegistry.LocalRoom;
                if (room != null && room.CurrentPlayerCount > 1)
                {
                    list.Add(new RoomPlayerDto
                    {
                        displayName = "Opponent",
                        username = "Opponent",
                        userUuid = "SIM-" + TournamentRoom.FormatShortId(room.roomId),
                        gameLevel = Mathf.Max(1, (GameLevelHolder.Instance ? GameLevelHolder.CurrentLevel + 1 : 1) +
                                                 UnityEngine.Random.Range(1, 40)),
                        rankTier = "Silver",
                        currentRank = 9999,
                        isConnected = true
                    });
                }
            }

            return list;
        }

        private static RoomPlayerDto BuildLocalFallbackPlayer()
        {
            int localLevel = GameLevelHolder.Instance ? GameLevelHolder.CurrentLevel + 1 : 1;
            string name = "You";
            string uuid = string.Empty;

            if (NetworkManager.HasInstance && !string.IsNullOrEmpty(NetworkManager.Instance.UserUuid))
                uuid = NetworkManager.Instance.UserUuid;

            return new RoomPlayerDto
            {
                userId = NetworkManager.HasInstance ? NetworkManager.Instance.UserId : 0,
                userUuid = uuid,
                username = name,
                displayName = name,
                gameLevel = localLevel,
                rankTier = "Bronze",
                currentRank = 9999,
                isConnected = true
            };
        }

        private static void PlayJoinSound()
        {
            if (SoundMaster.Instance)
                SoundMaster.Instance.SoundPlayClick(0.2f, null);
        }

        /// <summary>Used by VS intro overlay for player cards.</summary>
        public static PlayerCardView CreatePlayerCard(Transform parent, string name)
        {
            GameObject cardGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            cardGo.transform.SetParent(parent, false);
            Image bg = cardGo.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.12f, 0.08f, 0.94f);

            LayoutElement le = cardGo.GetComponent<LayoutElement>();
            le.minHeight = 180f;
            le.preferredHeight = 180f;

            RectTransform cardRt = cardGo.GetComponent<RectTransform>();

            GameObject avatarGo = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
            avatarGo.transform.SetParent(cardGo.transform, false);
            Image avatarImage = avatarGo.GetComponent<Image>();
            avatarImage.color = new Color(0.35f, 0.28f, 0.2f);
            RectTransform avatarRt = avatarImage.rectTransform;
            avatarRt.anchorMin = new Vector2(0.05f, 0.15f);
            avatarRt.anchorMax = new Vector2(0.28f, 0.85f);
            avatarRt.offsetMin = Vector2.zero;
            avatarRt.offsetMax = Vector2.zero;

            Text nameText = CreateText(cardGo.transform, "—", 26, FontStyle.Bold, Color.white);
            RectTransform nameRt = nameText.rectTransform;
            nameRt.anchorMin = new Vector2(0.32f, 0.72f);
            nameRt.anchorMax = new Vector2(0.95f, 0.9f);
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            nameText.alignment = TextAnchor.MiddleLeft;

            Text uuidText = CreateText(cardGo.transform, "UUID: —", 18, FontStyle.Normal, new Color(0.7f, 0.75f, 0.85f));
            RectTransform uuidRt = uuidText.rectTransform;
            uuidRt.anchorMin = new Vector2(0.32f, 0.48f);
            uuidRt.anchorMax = new Vector2(0.95f, 0.7f);
            uuidRt.offsetMin = Vector2.zero;
            uuidRt.offsetMax = Vector2.zero;
            uuidText.alignment = TextAnchor.UpperLeft;

            Text levelText = CreateText(cardGo.transform, "Level —", 20, FontStyle.Normal, new Color(0.95f, 0.88f, 0.55f));
            RectTransform levelRt = levelText.rectTransform;
            levelRt.anchorMin = new Vector2(0.32f, 0.28f);
            levelRt.anchorMax = new Vector2(0.62f, 0.46f);
            levelRt.offsetMin = Vector2.zero;
            levelRt.offsetMax = Vector2.zero;
            levelText.alignment = TextAnchor.MiddleLeft;

            Text rankText = CreateText(cardGo.transform, "Rank —", 20, FontStyle.Bold, new Color(0.75f, 0.9f, 1f));
            RectTransform rankRt = rankText.rectTransform;
            rankRt.anchorMin = new Vector2(0.62f, 0.28f);
            rankRt.anchorMax = new Vector2(0.95f, 0.46f);
            rankRt.offsetMin = Vector2.zero;
            rankRt.offsetMax = Vector2.zero;
            rankText.alignment = TextAnchor.MiddleRight;

            Text onlineText = CreateText(cardGo.transform, "SEARCHING", 18, FontStyle.Bold, new Color(0.55f, 0.55f, 0.6f));
            RectTransform onlineRt = onlineText.rectTransform;
            onlineRt.anchorMin = new Vector2(0.32f, 0.08f);
            onlineRt.anchorMax = new Vector2(0.95f, 0.26f);
            onlineRt.offsetMin = Vector2.zero;
            onlineRt.offsetMax = Vector2.zero;
            onlineText.alignment = TextAnchor.MiddleLeft;

            return new PlayerCardView
            {
                Root = cardRt,
                AvatarImage = avatarImage,
                NameText = nameText,
                UuidText = uuidText,
                LevelText = levelText,
                RankText = rankText,
                OnlineText = onlineText
            };
        }

        public class PlayerCardView
        {
            public RectTransform Root;
            public Image AvatarImage;
            public Text NameText;
            public Text UuidText;
            public Text LevelText;
            public Text RankText;
            public Text OnlineText;

            public void Bind(RoomPlayerDto player, bool isLocal, bool occupied, float searchPulse = 0f)
            {
                if (!occupied)
                {
                    NameText.text = "Searching for Player...";
                    UuidText.text = string.Empty;
                    LevelText.text = string.Empty;
                    RankText.text = string.Empty;
                    OnlineText.text = "Searching...";
                    OnlineText.color = new Color(1f, 0.85f, 0.35f, 0.9f);
                    float pulse = 0.55f + Mathf.PingPong(searchPulse * 2f, 0.35f);
                    AvatarImage.color = new Color(0.18f, 0.22f, 0.28f, pulse);
                    AvatarImage.sprite = null;
                    Root.localScale = Vector3.one * (1f + Mathf.PingPong(searchPulse * 1.5f, 0.04f));
                    return;
                }

                Root.localScale = Vector3.one;

                string displayName = !string.IsNullOrEmpty(player.username)
                    ? player.username
                    : player.displayName;
                if (isLocal && string.IsNullOrEmpty(displayName))
                    displayName = "You";

                NameText.text = displayName;
                UuidText.text = TournamentRankTier.FormatUuidLine(player.userUuid);
                LevelText.text = string.Empty;
                RankText.text = TournamentRankTier.FormatRankLine(player.currentRank ?? 0, player.rankTier);

                bool online = player.isConnected;
                OnlineText.text = online ? "● ONLINE" : "○ OFFLINE";
                OnlineText.color = online
                    ? new Color(0.35f, 1f, 0.55f)
                    : new Color(0.8f, 0.45f, 0.45f);

                AvatarImage.color = Color.white;
                TournamentAvatarLoader.Instance.Apply(AvatarImage, player.avatarUrl, isLocal);
            }
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
    }
}
