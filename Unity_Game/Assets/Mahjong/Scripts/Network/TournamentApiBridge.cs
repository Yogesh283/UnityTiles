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

        public static bool IsBackgroundJoinActive { get; private set; }

        public static bool IsReconnecting =>
            TournamentRoomWebSocket.IsReconnecting;

        public static event Action RoomUpdated;

        public static void SetBackgroundJoinActive(bool active) => IsBackgroundJoinActive = active;

        private static bool wsHooked;

        public static void ApplyJoinResponse(TournamentDefinition tournament, RoomResponseDto room)
        {
            if (tournament == null || room == null) return;

            ApplyRoomDto(tournament, room);
            EnsureWebSocket();

            if (HasMatchedRoom && !ApiConfig.Current.UseNakamaRealtimeNetworking)
                TournamentRoomWebSocket.Connect(CurrentRoom.roomId);
        }

        public static void ApplyRoomDto(TournamentDefinition tournament, RoomResponseDto room)
        {
            CurrentRoom = room;
            if (string.IsNullOrEmpty(room.roomId))
                return;

            ApplyServerRoomTiming(room);
            TournamentTransitionProbe.LogRoomSnapshot("ApplyRoomDto", GetApiSnapshot(tournament), tournament.maxPlayers);
            TournamentTransitionProbe.Step(4, "TournamentServerClock receives match_start_at_ms",
                (room.matchStartAtMs ?? 0) > 0,
                $"match_start_at_ms={room.matchStartAtMs?.ToString() ?? "null"} server_now_ms={room.serverNowMs?.ToString() ?? "null"}");

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
            TournamentFlowLog.RoomUpdated(
                $"status={room.status} players={room.playerCount} room={room.roomId}");
            RoomUpdated?.Invoke();
        }

        public static async Task<bool> RefreshActiveRoomAsync()
        {
            if (!HasMatchedRoom || !TournamentSession.IsActive)
                return false;

            var result = await TournamentService.FetchRoomSnapshotAsync(CurrentRoom.roomId);
            if (!result.Success || result.Data == null)
            {
                if (result.StatusCode == 404)
                {
                    if (!IsBackgroundJoinActive && !TournamentRoomWebSocket.IsReconnecting)
                        Clear();
                }
                else if (NetworkManager.IsTransientFailure(result))
                {
                    TournamentFlowLog.ApiRetry(
                        $"room refresh status={result.StatusCode} err={result.ErrorMessage}");
                }

                return false;
            }

            if (ApiConfig.Current.UseNakamaRealtimeNetworking)
                MergeIncomingRoomState(result.Data);
            else
                MergeIncomingRoomState(result.Data);

            ApplyRoomDto(TournamentSession.Tournament, CurrentRoom);
            return true;
        }

        public static void MergeAndNotify(RoomResponseDto incoming)
        {
            if (incoming == null || TournamentSession.Tournament == null)
                return;

            MergeIncomingRoomState(incoming);
            // ApplyRoomDto already fires RoomUpdated exactly once — do NOT invoke it again here or
            // every subscriber runs twice per merge (duplicate event => double UI refresh + GC).
            ApplyRoomDto(TournamentSession.Tournament, CurrentRoom);
        }

        public static void Clear()
        {
            if (wsHooked)
            {
                TournamentRoomWebSocket.MessageReceived -= OnWebSocketMessage;
                wsHooked = false;
            }

            TournamentRoomWebSocket.StopMaintainingConnection();
            CurrentRoom = null;
            TournamentServerClock.Reset();
        }

        public static TournamentRoomSnapshot GetApiSnapshot(TournamentDefinition tournament)
        {
            if (!HasActiveApiSession || tournament == null)
                return default;

            bool starting = CurrentRoom.status == "starting";
            bool active = CurrentRoom.status == "active" || CurrentRoom.status == "locked";

            bool shouldLaunch = starting || active;

            BuildPlayerLabels(CurrentRoom.players, out string localUuid, out string opponentUuid, out string opponentName,
                out string localName, out string opponentRankLine, out string localRankLine,
                out string localAvatarUrl, out string opponentAvatarUrl);

            string searchPhase = string.IsNullOrEmpty(CurrentRoom.searchStatus)
                ? ResolveSearchPhase(CurrentRoom.status, CurrentRoom.playerCount, tournament.maxPlayers)
                : CurrentRoom.searchStatus;

            float countdown = starting
                ? Mathf.Max(0f, CurrentRoom.startCountdownSeconds.GetValueOrDefault())
                : (active && (CurrentRoom.matchStartAtMs ?? 0) > 0
                    ? Mathf.Max(0f, (float)TournamentServerClock.SecondsUntilStart())
                    : Mathf.Max(0f, CurrentRoom.waitingSecondsRemaining.GetValueOrDefault()));

            return new TournamentRoomSnapshot
            {
                hasRoom = true,
                roomId = CurrentRoom.roomId,
                currentPlayers = Mathf.Max(1, CurrentRoom.playerCount),
                maxPlayers = tournament.maxPlayers,
                countdownSeconds = countdown,
                startCountdownSeconds = CurrentRoom.startCountdownSeconds.GetValueOrDefault(),
                matchStartAtMs = CurrentRoom.matchStartAtMs.GetValueOrDefault(),
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

            if (ApiConfig.Current.UseNakamaRealtimeNetworking)
                TournamentRoomWebSocket.EnsureNakamaMessageBridge();

            TournamentRoomWebSocket.MessageReceived += OnWebSocketMessage;
            wsHooked = true;
        }

        private static void OnWebSocketMessage(string json)
        {
            try
            {
                JObject payload = JObject.Parse(json);
                string eventName = payload.Value<string>("event");
                if (string.IsNullOrEmpty(eventName)) return;

                TournamentFlowLog.Event(eventName, json.Length > 120 ? json.Substring(0, 120) + "..." : json);

                JToken roomToken = payload["room"];
                RoomResponseDto previewRoom = roomToken?.ToObject<RoomResponseDto>();
                if (previewRoom != null &&
                    (eventName == "room_updated" || eventName == "match_start" || eventName == "countdown"))
                    TournamentTransitionProbe.LogWsEvent(eventName, previewRoom);

                if (eventName == "match_finished")
                {
                    TournamentFlowLog.MatchFinished("received");
                    TournamentMatchManager.HandleServerMatchFinished(payload["results"]);
                    return;
                }

                if (roomToken == null)
                    return;

                if (eventName == "player_joined")
                {
                    TournamentFlowLog.RoomJoined($"room={CurrentRoom?.roomId} players={previewRoom?.playerCount}");
                    TournamentFlowLog.PlayerJoined("opponent connected via WebSocket");
                    if (previewRoom != null && previewRoom.playerCount >= 2)
                        TournamentFlowLog.PlayerFound($"players={previewRoom.playerCount}");
                }
                else if (eventName == "countdown")
                {
                    RoomResponseDto countdownRoom = roomToken.ToObject<RoomResponseDto>();
                    if (countdownRoom != null)
                    {
                        TournamentFlowLog.CountdownStart(
                            $"status={countdownRoom.status} players={countdownRoom.playerCount} " +
                            $"remaining={countdownRoom.startCountdownSeconds} match_start_at_ms={countdownRoom.matchStartAtMs}");
                        TournamentFlowLog.Countdown(
                            $"status={countdownRoom.status} players={countdownRoom.playerCount} " +
                            $"remaining={countdownRoom.startCountdownSeconds} match_start_at_ms={countdownRoom.matchStartAtMs}");
                        TournamentFlowLog.WaitingState("starting", "countdown event from server");
                    }
                }
                else if (eventName == "match_start")
                {
                    TournamentFlowLog.GameStart($"WebSocket match_start room={CurrentRoom?.roomId}");
                    TournamentFlowLog.MatchStart("WebSocket match_start");
                    TournamentFlowLog.WaitingState("active", "match_start event from server");
                }
                else if (eventName == "room_updated")
                {
                    RoomResponseDto updated = roomToken.ToObject<RoomResponseDto>();
                    if (updated != null)
                        TournamentFlowLog.WaitingState(
                            updated.status ?? "unknown",
                            $"players={updated.playerCount} room_updated");
                }

                RoomResponseDto room = roomToken.ToObject<RoomResponseDto>();
                if (room == null || TournamentSession.Tournament == null) return;

                MergeIncomingRoomState(room);
                // ApplyRoomDto fires RoomUpdated once; a second explicit invoke here made every WS
                // message trigger duplicate handling (duplicate RoomUpdated event). Removed.
                ApplyRoomDto(TournamentSession.Tournament, CurrentRoom);

                if ((eventName == "match_start" || eventName == "countdown" || eventName == "room_updated") &&
                    (CurrentRoom.matchStartAtMs ?? 0) > 0)
                {
                    TournamentServerClock.ScheduleServerStart(CurrentRoom.matchStartAtMs.Value);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TournamentApiBridge] WS parse failed: " + ex.Message);
            }
        }

        private static void MergeIncomingRoomState(RoomResponseDto incoming)
        {
            if (CurrentRoom == null)
            {
                CurrentRoom = incoming;
                ApplyServerRoomTiming(CurrentRoom);
                return;
            }

            if (incoming == null)
                return;

            if (ShouldIgnoreStaleIncoming(incoming))
                return;

            CurrentRoom.roomId = incoming.roomId ?? CurrentRoom.roomId;
            CurrentRoom.tournamentId = incoming.tournamentId ?? CurrentRoom.tournamentId;
            CurrentRoom.tournamentName = incoming.tournamentName ?? CurrentRoom.tournamentName;
            CurrentRoom.levelIndex = incoming.levelIndex;
            CurrentRoom.levelSeed = incoming.levelSeed;
            CurrentRoom.maxPlayers = incoming.maxPlayers > 0 ? incoming.maxPlayers : CurrentRoom.maxPlayers;
            CurrentRoom.waitingSeconds = incoming.waitingSeconds;
            CurrentRoom.waitingSecondsRemaining = incoming.waitingSecondsRemaining;
            if (incoming.walletBalance.HasValue)
                CurrentRoom.walletBalance = incoming.walletBalance;

            CurrentRoom.playerCount = Mathf.Max(CurrentRoom.playerCount, incoming.playerCount);

            if (incoming.status == "finished" || incoming.status == "locked")
                CurrentRoom.status = incoming.status;
            else if (StatusRank(incoming.status) > StatusRank(CurrentRoom.status))
                CurrentRoom.status = incoming.status ?? CurrentRoom.status;

            if ((incoming.matchStartAtMs ?? 0) > 0)
                CurrentRoom.matchStartAtMs = incoming.matchStartAtMs;
            if ((incoming.serverNowMs ?? 0) > 0)
                CurrentRoom.serverNowMs = incoming.serverNowMs;
            if (!string.IsNullOrEmpty(incoming.searchStatus))
                CurrentRoom.searchStatus = incoming.searchStatus;
            if (incoming.startCountdownSeconds.HasValue)
                CurrentRoom.startCountdownSeconds = incoming.startCountdownSeconds;

            if (incoming.players != null &&
                (incoming.players.Count > (CurrentRoom.players?.Count ?? 0) ||
                 incoming.playerCount > CurrentRoom.playerCount ||
                 incoming.status == "finished" ||
                 HasRankedPlayers(incoming.players) ||
                 incoming.playerCount >= CurrentRoom.maxPlayers))
                CurrentRoom.players = incoming.players;

            ApplyServerRoomTiming(CurrentRoom);
        }

        private static bool ShouldIgnoreStaleIncoming(RoomResponseDto incoming)
        {
            if (CurrentRoom == null || incoming == null)
                return false;

            long incomingNow = incoming.serverNowMs ?? 0;
            long currentNow = CurrentRoom.serverNowMs ?? 0;
            if (incomingNow <= 0 || currentNow <= 0)
                return false;

            if (incomingNow >= currentNow)
                return false;

            bool staleStatus = StatusRank(incoming.status) < StatusRank(CurrentRoom.status);
            bool stalePlayers = incoming.playerCount < CurrentRoom.playerCount;
            return staleStatus && stalePlayers;
        }

        private static void MergeRoomState(RoomResponseDto incoming) => MergeIncomingRoomState(incoming);

        private static void MergeFastApiBusinessState(RoomResponseDto incoming) => MergeIncomingRoomState(incoming);

        private static bool HasRankedPlayers(List<RoomPlayerDto> players)
        {
            if (players == null)
                return false;

            foreach (RoomPlayerDto player in players)
            {
                if (player != null && player.rank > 0)
                    return true;
            }

            return false;
        }

        private static int StatusRank(string status)
        {
            switch (status)
            {
                case "waiting": return 1;
                case "starting": return 2;
                case "active": return 3;
                case "locked": return 4;
                case "finished": return 5;
                default: return 0;
            }
        }

        private static void ApplyServerRoomTiming(RoomResponseDto room)
        {
            if (room == null) return;

            if ((room.serverNowMs ?? 0) > 0)
                TournamentServerClock.SyncServerTime(room.serverNowMs.Value);

            if ((room.matchStartAtMs ?? 0) > 0)
                TournamentServerClock.ScheduleServerStart(room.matchStartAtMs.Value);
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

                int currentRank = player.currentRank ?? 0;
                string rankLine = !string.IsNullOrEmpty(player.rankTier)
                    ? TournamentRankTier.FormatRankLine(currentRank, player.rankTier)
                    : (currentRank > 0 && currentRank < 9999
                        ? $"Rank #{currentRank}"
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
