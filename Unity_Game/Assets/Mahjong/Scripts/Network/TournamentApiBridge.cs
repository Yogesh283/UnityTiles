using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mkey.Tournament;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mkey.Network
{
    /// <summary>
    /// Bridges API + WebSocket room data into the tournament room registry.
    /// </summary>
    public static class TournamentApiBridge
    {
        public static bool IsOnlineMode => !ApiConfig.Current.UseLocalSimulation;

        public static RoomResponseDto CurrentRoom { get; private set; }

        public static bool HasActiveApiSession =>
            IsOnlineMode && CurrentRoom != null;

        public static bool HasMatchedRoom =>
            HasActiveApiSession && !string.IsNullOrEmpty(CurrentRoom.roomId);

        public static bool HasActiveApiRoom => HasMatchedRoom;

        public static event Action RoomUpdated;

        private static bool wsHooked;

        public static void ApplyJoinResponse(TournamentDefinition tournament, RoomResponseDto room)
        {
            if (tournament == null || room == null) return;

            ApplyRoomDto(tournament, room);
            EnsureWebSocket();
        }

        public static void ApplyRoomDto(TournamentDefinition tournament, RoomResponseDto room)
        {
            CurrentRoom = room;
            if (string.IsNullOrEmpty(room.roomId))
                return;

            ApplyServerRoomTiming(room);

            TournamentRoom registryRoom = TournamentRoomRegistry.JoinOrGetRoom(tournament);
            registryRoom?.ApplyApiRoomData(
                room.roomId,
                room.levelIndex,
                room.levelSeed,
                room.playerCount,
                room.status,
                room.waitingSeconds);
            registryRoom?.ApplyOnlinePlayers(room.players);
            TournamentSession.BindRoom(room.roomId, room.levelIndex, room.levelSeed);
            RoomUpdated?.Invoke();
        }

        public static async Task<bool> RefreshActiveRoomAsync()
        {
            if (!HasMatchedRoom) return false;

            var result = await TournamentService.FetchRoomSnapshotAsync(CurrentRoom.roomId);
            if (!result.Success || result.Data == null) return false;

            MergeRoomState(result.Data);
            ApplyRoomDto(TournamentSession.Tournament, CurrentRoom);
            return true;
        }

        public static void Clear()
        {
            if (wsHooked)
            {
                TournamentRoomWebSocket.MessageReceived -= OnWebSocketMessage;
                wsHooked = false;
            }

            TournamentRoomWebSocket.Disconnect();
            CurrentRoom = null;
            TournamentServerClock.Reset();
        }

        public static TournamentRoomSnapshot GetApiSnapshot(TournamentDefinition tournament)
        {
            if (!HasActiveApiSession || tournament == null)
                return default;

            bool starting = CurrentRoom.status == "starting";
            bool active = CurrentRoom.status == "active" || CurrentRoom.status == "locked";

            // Never launch while still searching — only when server begins match countdown.
            bool shouldLaunch = starting || active;

            BuildPlayerLabels(CurrentRoom.players, out string localUuid, out string opponentUuid, out string opponentName,
                out string localName, out string opponentRankLine, out string localRankLine,
                out string localAvatarUrl, out string opponentAvatarUrl);

            string searchPhase = string.IsNullOrEmpty(CurrentRoom.searchStatus)
                ? ResolveSearchPhase(CurrentRoom.status, CurrentRoom.playerCount, tournament.maxPlayers)
                : CurrentRoom.searchStatus;

            float countdown = starting
                ? Mathf.Max(0f, CurrentRoom.startCountdownSeconds)
                : Mathf.Max(0f, CurrentRoom.waitingSecondsRemaining);

            return new TournamentRoomSnapshot
            {
                hasRoom = true,
                roomId = CurrentRoom.roomId,
                currentPlayers = Mathf.Max(1, CurrentRoom.playerCount),
                maxPlayers = tournament.maxPlayers,
                countdownSeconds = countdown,
                startCountdownSeconds = CurrentRoom.startCountdownSeconds,
                matchStartAtMs = CurrentRoom.matchStartAtMs,
                status = CurrentRoom.status,
                searchStatus = searchPhase,
                statusMessage = GetStatusMessage(searchPhase, CurrentRoom.playerCount, tournament.maxPlayers),
                shouldLaunch = shouldLaunch,
                localPlayerUuid = localUuid,
                localPlayerName = localName,
                localPlayerRankLine = localRankLine,
                localPlayerAvatarUrl = localAvatarUrl,
                opponentUuid = opponentUuid,
                opponentName = opponentName,
                opponentRankLine = opponentRankLine,
                opponentAvatarUrl = opponentAvatarUrl,
                players = CurrentRoom.players
            };
        }

        private static string ResolveSearchPhase(string status, int players, int maxPlayers)
        {
            if (status == "starting") return "match_found";
            if (status == "active" || status == "locked") return "starting";
            if (players >= maxPlayers) return "players_connected";
            if (players >= 2) return "player_joined";
            return "searching";
        }

        private static void EnsureWebSocket()
        {
            if (!HasMatchedRoom || wsHooked) return;
            TournamentRoomWebSocket.MessageReceived += OnWebSocketMessage;
            wsHooked = true;
            TournamentRoomWebSocket.Connect(CurrentRoom.roomId);
        }

        private static void OnWebSocketMessage(string json)
        {
            try
            {
                JObject payload = JObject.Parse(json);
                string eventName = payload.Value<string>("event");
                if (string.IsNullOrEmpty(eventName)) return;

                JToken roomToken = payload["room"];
                if (roomToken == null && eventName != "match_finished") return;

                if (eventName == "match_finished")
                {
                    TournamentMatchManager.HandleServerMatchFinished(payload["results"]);
                    return;
                }

                if (eventName == "player_joined" && payload["player"] != null)
                    Debug.Log("[TournamentApiBridge] Real player joined via WebSocket.");

                RoomResponseDto room = roomToken.ToObject<RoomResponseDto>();
                if (room == null || TournamentSession.Tournament == null) return;

                MergeRoomState(room);
                ApplyRoomDto(TournamentSession.Tournament, CurrentRoom);
                RoomUpdated?.Invoke();

                if (eventName == "match_start" && CurrentRoom.matchStartAtMs > 0)
                    TournamentServerClock.ScheduleServerStart(CurrentRoom.matchStartAtMs);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TournamentApiBridge] WS parse failed: " + ex.Message);
            }
        }

        private static void MergeRoomState(RoomResponseDto incoming)
        {
            if (CurrentRoom == null)
            {
                CurrentRoom = incoming;
                ApplyServerRoomTiming(CurrentRoom);
                return;
            }

            CurrentRoom.roomId = incoming.roomId ?? CurrentRoom.roomId;
            CurrentRoom.tournamentId = incoming.tournamentId ?? CurrentRoom.tournamentId;
            CurrentRoom.tournamentName = incoming.tournamentName ?? CurrentRoom.tournamentName;
            CurrentRoom.levelIndex = incoming.levelIndex;
            CurrentRoom.levelSeed = incoming.levelSeed;
            CurrentRoom.status = incoming.status ?? CurrentRoom.status;
            CurrentRoom.playerCount = incoming.playerCount;
            CurrentRoom.maxPlayers = incoming.maxPlayers > 0 ? incoming.maxPlayers : CurrentRoom.maxPlayers;
            CurrentRoom.waitingSeconds = incoming.waitingSeconds;
            CurrentRoom.waitingSecondsRemaining = incoming.waitingSecondsRemaining;
            CurrentRoom.startCountdownSeconds = incoming.startCountdownSeconds;
            CurrentRoom.searchStatus = incoming.searchStatus ?? CurrentRoom.searchStatus;
            if (incoming.matchStartAtMs > 0)
                CurrentRoom.matchStartAtMs = incoming.matchStartAtMs;
            if (incoming.serverNowMs > 0)
                CurrentRoom.serverNowMs = incoming.serverNowMs;
            if (incoming.players != null)
                CurrentRoom.players = incoming.players;

            ApplyServerRoomTiming(CurrentRoom);
        }

        private static void ApplyServerRoomTiming(RoomResponseDto room)
        {
            if (room == null) return;

            if (room.serverNowMs > 0)
                TournamentServerClock.SyncServerTime(room.serverNowMs);

            if (room.matchStartAtMs > 0)
                TournamentServerClock.ScheduleServerStart(room.matchStartAtMs);
        }

        private static void BuildPlayerLabels(
            List<RoomPlayerDto> players,
            out string localUuid,
            out string opponentUuid,
            out string opponentName,
            out string localName,
            out string opponentRankLine,
            out string localRankLine,
            out string localAvatarUrl,
            out string opponentAvatarUrl)
        {
            localUuid = NetworkManager.HasInstance ? NetworkManager.Instance.UserUuid : string.Empty;
            opponentUuid = string.Empty;
            opponentName = string.Empty;
            localName = string.Empty;
            opponentRankLine = string.Empty;
            localRankLine = string.Empty;
            localAvatarUrl = string.Empty;
            opponentAvatarUrl = string.Empty;

            if (players == null || players.Count == 0) return;

            int localUserId = NetworkManager.HasInstance ? NetworkManager.Instance.UserId : 0;
            foreach (RoomPlayerDto player in players)
            {
                if (player == null) continue;

                string rankLine = !string.IsNullOrEmpty(player.rankTier)
                    ? TournamentRankTier.FormatRankLine(player.currentRank, player.rankTier)
                    : (player.currentRank > 0 && player.currentRank < 9999
                        ? $"Rank #{player.currentRank}"
                        : "Rank —");

                if (player.userId == localUserId)
                {
                    if (!string.IsNullOrEmpty(player.userUuid))
                        localUuid = player.userUuid;
                    localName = string.IsNullOrEmpty(player.displayName) ? "You" : player.displayName;
                    localRankLine = rankLine;
                    localAvatarUrl = player.avatarUrl ?? string.Empty;
                    continue;
                }

                opponentUuid = !string.IsNullOrEmpty(player.userUuid)
                    ? player.userUuid
                    : "player_" + player.userId;
                opponentName = string.IsNullOrEmpty(player.displayName)
                    ? TournamentRoom.FormatShortId(opponentUuid)
                    : player.displayName;
                opponentRankLine = rankLine;
                opponentAvatarUrl = player.avatarUrl ?? string.Empty;
            }
        }

        private static string GetStatusMessage(string searchPhase, int players, int maxPlayers)
        {
            if (searchPhase == "match_found" || searchPhase == "starting") return "Match Found!";
            if (players >= maxPlayers) return "Player Found!";
            if (players >= 2) return "Player Joined";
            return "Searching for Players...";
        }
    }
}
