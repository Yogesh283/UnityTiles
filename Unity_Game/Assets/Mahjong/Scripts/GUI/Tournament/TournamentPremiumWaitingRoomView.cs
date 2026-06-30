using System.Collections.Generic;
using Mkey;
using Mkey.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Premium multiplayer waiting room overlay — player cards, room info, live search status.
    /// </summary>
    public class TournamentPremiumWaitingRoomView : MonoBehaviour
    {
        private Text titleText;
        private Text roomIdText;
        private Text playersText;
        private Text statusText;
        private Text searchText;
        private Text connectionText;
        private Text timerText;
        private Image searchPulseImage;
        private RectTransform duelRow;
        private RectTransform listRoot;
        private readonly List<PlayerCardView> duelCards = new List<PlayerCardView>();
        private readonly List<PlayerCardView> listCards = new List<PlayerCardView>();
        private int lastPlayerCount;
        private bool visible;

        public bool IsVisible => visible;

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
            view.BuildUi();
            host.SetActive(false);
            return view;
        }

        public void Show()
        {
            visible = true;
            gameObject.SetActive(true);
            lastPlayerCount = 0;
        }

        public void Hide()
        {
            visible = false;
            gameObject.SetActive(false);
        }

        public void Bind(TournamentDefinition tournament, TournamentRoomSnapshot snap, float searchPulse)
        {
            if (tournament == null) return;

            string phase = string.IsNullOrEmpty(snap.searchStatus) ? "searching" : snap.searchStatus;
            int current = snap.hasRoom ? snap.currentPlayers : 1;
            int max = snap.maxPlayers > 0 ? snap.maxPlayers : tournament.maxPlayers;

            titleText.text = $"{tournament.icon} {tournament.displayName}";
            roomIdText.text = snap.hasRoom && !string.IsNullOrEmpty(snap.roomId)
                ? $"Room ID: {TournamentRoom.FormatShortId(snap.roomId)}"
                : "Room ID: —";

            playersText.text = $"{current} / {max} Players";
            connectionText.text = TournamentApiBridge.IsOnlineMode
                ? (TournamentRoomWebSocket.IsConnected ? "● Connected — Live" : "○ Connecting...")
                : "● Local Practice";

            if (snap.status == "starting" || phase == "match_found")
            {
                statusText.text = "MATCH FOUND";
                statusText.color = new Color(0.4f, 1f, 0.55f);
                int cd = TournamentServerClock.DisplayCountdownSeconds();
                timerText.text = cd > 0 ? $"Starting in {cd}..." : "GO!";
                searchText.text = "Get ready!";
            }
            else if (phase == "players_connected" || current >= max)
            {
                statusText.text = "ROOM FULL";
                statusText.color = new Color(1f, 0.85f, 0.35f);
                timerText.text = FormatWaitTimer(snap);
                searchText.text = TournamentPlayerSearchPresenter.StatusForPhase("players_connected", 0);
            }
            else if (phase == "player_joined" || current >= 2)
            {
                statusText.text = "PLAYER JOINED";
                statusText.color = new Color(0.55f, 0.85f, 1f);
                timerText.text = FormatWaitTimer(snap);
                searchText.text = TournamentPlayerSearchPresenter.StatusForPhase("player_joined", 0);
            }
            else
            {
                statusText.text = "SEARCHING";
                statusText.color = new Color(1f, 0.92f, 0.55f);
                timerText.text = FormatWaitTimer(snap);
                int dots = 1 + (Mathf.FloorToInt(searchPulse * 2f) % 3);
                searchText.text = TournamentPlayerSearchPresenter.StatusForPhase("searching", dots);
            }

            if (searchPulseImage)
            {
                float a = 0.15f + Mathf.PingPong(searchPulse * 1.5f, 0.2f);
                searchPulseImage.color = new Color(1f, 0.82f, 0.2f, a);
            }

            List<RoomPlayerDto> players = BuildPlayerList(snap, tournament, current, max);
            bool duelLayout = max <= 2;
            duelRow.gameObject.SetActive(duelLayout);
            listRoot.gameObject.SetActive(!duelLayout);

            if (duelLayout)
                RefreshDuelCards(players, max);
            else
                RefreshListCards(players, max);

            if (current > lastPlayerCount && lastPlayerCount > 0)
                PlayJoinSound();

            lastPlayerCount = current;
        }

        private static string FormatWaitTimer(TournamentRoomSnapshot snap)
        {
            float displayTime = snap.hasRoom ? snap.countdownSeconds : 0f;
            int minutes = Mathf.FloorToInt(displayTime / 60f);
            int seconds = Mathf.FloorToInt(displayTime % 60f);
            return $"{minutes:00}:{seconds:00}";
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

            while (list.Count < max)
                list.Add(null);

            return list;
        }

        private static RoomPlayerDto BuildLocalFallbackPlayer()
        {
            int localLevel = GameLevelHolder.Instance ? GameLevelHolder.CurrentLevel + 1 : 1;
            string name = "You";
            string uuid = string.Empty;

            if (NetworkManager.HasInstance)
            {
                if (!string.IsNullOrEmpty(NetworkManager.Instance.UserUuid))
                    uuid = NetworkManager.Instance.UserUuid;
            }

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

        private void RefreshDuelCards(List<RoomPlayerDto> players, int max)
        {
            EnsureDuelCards(max);
            float[] anchors = { 0f, 0.58f };
            float[] anchorMax = { 0.38f, 1f };

            for (int i = 0; i < duelCards.Count; i++)
            {
                if (i >= max)
                {
                    duelCards[i].Root.gameObject.SetActive(false);
                    continue;
                }

                duelCards[i].Root.gameObject.SetActive(true);
                RectTransform rt = duelCards[i].Root;
                rt.anchorMin = new Vector2(anchors[i], 0f);
                rt.anchorMax = new Vector2(anchorMax[i], 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                RoomPlayerDto dto = i < players.Count ? players[i] : null;
                bool isLocal = dto != null && IsLocalPlayer(dto);
                duelCards[i].Bind(dto, isLocal, dto != null);
            }
        }

        private void RefreshListCards(List<RoomPlayerDto> players, int max)
        {
            EnsureListCards(Mathf.Min(max, Mathf.Max(players.Count, 4)));
            for (int i = 0; i < listCards.Count; i++)
            {
                RoomPlayerDto dto = i < players.Count ? players[i] : null;
                bool isLocal = dto != null && IsLocalPlayer(dto);
                listCards[i].Bind(dto, isLocal, dto != null);
            }
        }

        private void EnsureDuelCards(int max)
        {
            while (duelCards.Count < max)
            {
                PlayerCardView card = CreatePlayerCard(duelRow, $"Duel_{duelCards.Count}");
                card.Root.SetParent(duelRow, false);
                duelCards.Add(card);
            }
        }

        private void EnsureListCards(int count)
        {
            while (listCards.Count < count)
            {
                PlayerCardView card = CreatePlayerCard(listRoot, $"List_{listCards.Count}");
                listCards.Add(card);
            }

            for (int i = 0; i < listCards.Count; i++)
                listCards[i].Root.gameObject.SetActive(i < count);
        }

        private static bool IsLocalPlayer(RoomPlayerDto dto)
        {
            if (dto == null) return false;
            if (NetworkManager.HasInstance && dto.userId == NetworkManager.Instance.UserId)
                return true;
            return dto.displayName == "You";
        }

        private static void PlayJoinSound()
        {
            if (SoundMaster.Instance)
                SoundMaster.Instance.SoundPlayClick(0.2f, null);
        }

        private void BuildUi()
        {
            Image backdrop = CreateImage(transform, "Backdrop", new Color(0.02f, 0.06f, 0.05f, 0.96f));
            Stretch(backdrop.rectTransform);

            Image panel = CreateImage(transform, "Panel", new Color(0.06f, 0.14f, 0.1f, 0.98f));
            RectTransform panelRt = panel.rectTransform;
            panelRt.anchorMin = new Vector2(0.04f, 0.06f);
            panelRt.anchorMax = new Vector2(0.96f, 0.94f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            searchPulseImage = CreateImage(panel.transform, "SearchPulse", new Color(1f, 0.82f, 0.2f, 0.12f));
            Stretch(searchPulseImage.rectTransform);

            titleText = CreateText(panel.transform, "Tournament", 42, FontStyle.Bold, TournamentPremiumTheme.GoldBright);
            LayoutTop(titleText.rectTransform, 0.9f, 0.98f);

            roomIdText = CreateText(panel.transform, "Room ID", 22, FontStyle.Normal, new Color(0.75f, 0.75f, 0.8f));
            LayoutTop(roomIdText.rectTransform, 0.83f, 0.9f);

            playersText = CreateText(panel.transform, "1 / 2 Players", 28, FontStyle.Bold, Color.white);
            LayoutTop(playersText.rectTransform, 0.76f, 0.83f);

            statusText = CreateText(panel.transform, "SEARCHING", 34, FontStyle.Bold, new Color(1f, 0.92f, 0.55f));
            LayoutTop(statusText.rectTransform, 0.68f, 0.76f);

            searchText = CreateText(panel.transform, "Searching...", 24, FontStyle.Italic, new Color(0.9f, 0.9f, 0.95f));
            LayoutTop(searchText.rectTransform, 0.61f, 0.68f);

            timerText = CreateText(panel.transform, "00:12", 30, FontStyle.Bold, Color.white);
            LayoutTop(timerText.rectTransform, 0.54f, 0.61f);

            connectionText = CreateText(panel.transform, "Connecting...", 20, FontStyle.Normal, new Color(0.5f, 1f, 0.65f));
            LayoutTop(connectionText.rectTransform, 0.02f, 0.08f);

            GameObject duelHost = new GameObject("DuelRow", typeof(RectTransform));
            duelHost.transform.SetParent(panel.transform, false);
            duelRow = duelHost.GetComponent<RectTransform>();
            duelRow.anchorMin = new Vector2(0.04f, 0.12f);
            duelRow.anchorMax = new Vector2(0.96f, 0.52f);
            duelRow.offsetMin = Vector2.zero;
            duelRow.offsetMax = Vector2.zero;

            GameObject vsLabel = new GameObject("VsLabel", typeof(RectTransform));
            vsLabel.transform.SetParent(duelRow, false);
            RectTransform vsRt = vsLabel.GetComponent<RectTransform>();
            vsRt.anchorMin = new Vector2(0.42f, 0.35f);
            vsRt.anchorMax = new Vector2(0.58f, 0.65f);
            vsRt.offsetMin = Vector2.zero;
            vsRt.offsetMax = Vector2.zero;
            Text vs = CreateText(vsLabel.transform, "VS", 48, FontStyle.Bold, new Color(1f, 0.82f, 0.25f));
            Stretch(vs.rectTransform);

            GameObject listHost = new GameObject("List", typeof(RectTransform));
            listHost.transform.SetParent(panel.transform, false);
            listRoot = listHost.GetComponent<RectTransform>();
            listRoot.anchorMin = new Vector2(0.06f, 0.12f);
            listRoot.anchorMax = new Vector2(0.94f, 0.52f);
            listRoot.offsetMin = Vector2.zero;
            listRoot.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = listHost.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
        }

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

            public void Bind(RoomPlayerDto player, bool isLocal, bool occupied)
            {
                if (!occupied)
                {
                    NameText.text = "Searching...";
                    UuidText.text = "Waiting for player";
                    LevelText.text = string.Empty;
                    RankText.text = string.Empty;
                    OnlineText.text = "○ OFFLINE";
                    OnlineText.color = new Color(0.55f, 0.55f, 0.6f);
                    AvatarImage.color = new Color(0.2f, 0.2f, 0.24f);
                    AvatarImage.sprite = null;
                    return;
                }

                string displayName = !string.IsNullOrEmpty(player.username)
                    ? player.username
                    : player.displayName;
                if (isLocal && string.IsNullOrEmpty(displayName))
                    displayName = "You";

                NameText.text = $"🙂 {displayName}";
                UuidText.text = TournamentRankTier.FormatUuidLine(player.userUuid);
                LevelText.text = TournamentRankTier.FormatLevelLine(
                    player.gameLevel > 0
                        ? player.gameLevel
                        : (isLocal && GameLevelHolder.Instance ? GameLevelHolder.CurrentLevel + 1 : 1));
                RankText.text = TournamentRankTier.FormatRankLine(player.currentRank, player.rankTier);

                bool online = player.isConnected;
                OnlineText.text = online ? "● ONLINE" : "○ OFFLINE";
                OnlineText.color = online
                    ? new Color(0.35f, 1f, 0.55f)
                    : new Color(0.8f, 0.45f, 0.45f);

                AvatarImage.color = Color.white;
                TournamentAvatarLoader.Instance.Apply(AvatarImage, player.avatarUrl, isLocal);
            }
        }

        private static void LayoutTop(RectTransform rt, float yMin, float yMax)
        {
            rt.anchorMin = new Vector2(0.06f, yMin);
            rt.anchorMax = new Vector2(0.94f, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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
