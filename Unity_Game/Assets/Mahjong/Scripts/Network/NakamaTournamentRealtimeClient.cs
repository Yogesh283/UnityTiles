using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mkey.Tournament;
using Nakama;
using Newtonsoft.Json;
using UnityEngine;

namespace Mkey.Network
{
    /// <summary>
    /// Feature-flagged Nakama realtime adapter for tournament room lifecycle.
    /// Keeps existing FastAPI business APIs untouched.
    /// </summary>
    public sealed class NakamaTournamentRealtimeClient : MonoBehaviour
    {
        private const int OpRoomState = 10;
        private const int OpMatchStart = 11;
        private const int OpMatchFinished = 12;
        private const int OpReportFinish = 13;
        private const int MatchmakerMinPlayersDefault = 2;

        private static NakamaTournamentRealtimeClient instance;

        private IClient client;
        private ISession session;
        private ISocket socket;
        private ISocket hookedSocket;
        private IMatch currentMatch;
        private RoomResponseDto businessRoom;
        private string lastKnownStatus = "waiting";
        private readonly Dictionary<string, RoomPlayerDto> playersByNakamaUserId = new Dictionary<string, RoomPlayerDto>();

        // Nakama socket callbacks fire on a background thread. Room events are queued here and
        // drained in Update() so all downstream consumers touch Unity APIs on the main thread only.
        private readonly ConcurrentQueue<string> mainThreadEvents = new ConcurrentQueue<string>();

        public static event Action<string> MessageReceived;

        public static string ActiveRoomId =>
            instance != null && instance.businessRoom != null && !string.IsNullOrEmpty(instance.businessRoom.roomId)
                ? instance.businessRoom.roomId
                : instance != null && instance.currentMatch != null
                    ? instance.currentMatch.Id
                    : null;

        public static bool IsConnected => instance != null && instance.socket != null && instance.socket.IsConnected;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance) return;
            GameObject host = new GameObject(nameof(NakamaTournamentRealtimeClient));
            instance = host.AddComponent<NakamaTournamentRealtimeClient>();
            DontDestroyOnLoad(host);
        }

        private void Update()
        {
            // Drain background-thread Nakama events on the Unity main thread.
            while (mainThreadEvents.TryDequeue(out string json))
                MessageReceived?.Invoke(json);
        }

        public static async Task<ApiResult<RoomResponseDto>> MatchmakeAndJoinAsync(
            TournamentDefinition tournament,
            RoomResponseDto businessRoomDto,
            int timeoutMs = 20000)
        {
            Bootstrap();
            if (tournament == null)
                return ApiResult<RoomResponseDto>.Fail("Tournament missing.");

            if (businessRoomDto == null || string.IsNullOrEmpty(businessRoomDto.roomId))
                return ApiResult<RoomResponseDto>.Fail("FastAPI room missing — business join required before Nakama realtime.");

            try
            {
                instance.businessRoom = businessRoomDto;
                instance.lastKnownStatus = businessRoomDto.status ?? "waiting";
                await instance.EnsureConnectedAsync();

                int targetPlayers = Mathf.Max(MatchmakerMinPlayersDefault, tournament.maxPlayers);
                string query = "+properties.tournament_id:" + tournament.id;

                TaskCompletionSource<IMatchmakerMatched> matched = new TaskCompletionSource<IMatchmakerMatched>();
                void OnMatched(IMatchmakerMatched data) => matched.TrySetResult(data);
                instance.socket.ReceivedMatchmakerMatched += OnMatched;

                try
                {
                    await instance.socket.AddMatchmakerAsync(
                        query,
                        MatchmakerMinPlayersDefault,
                        targetPlayers,
                        stringProperties: new Dictionary<string, string>
                        {
                            { "tournament_id", tournament.id },
                            { "max_players", targetPlayers.ToString() },
                            { "fastapi_room_id", businessRoomDto.roomId },
                            { "app_user_id", NetworkManager.Instance.UserId.ToString() },
                            { "app_user_uuid", NetworkManager.Instance.UserUuid ?? string.Empty },
                            { "display_name", "Player " + NetworkManager.Instance.UserId }
                        });

                    IMatchmakerMatched result = await WithTimeout(matched.Task, timeoutMs);
                    instance.currentMatch = await instance.socket.JoinMatchAsync(result);
                }
                finally
                {
                    instance.socket.ReceivedMatchmakerMatched -= OnMatched;
                }

                instance.RebuildPlayersFromMatchPresences();

                RoomResponseDto initial = instance.BuildRoomFromCurrentState(
                    tournament,
                    status: instance.lastKnownStatus,
                    startCountdownSeconds: businessRoomDto.startCountdownSeconds,
                    matchStartAtMs: businessRoomDto.matchStartAtMs);
                initial.walletBalance = businessRoomDto.walletBalance;
                instance.EmitRoomUpdated(initial);

                return ApiResult<RoomResponseDto>.Ok(initial);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NakamaTournament] Matchmake/join failed: " + ex.Message);
                return ApiResult<RoomResponseDto>.Fail(ex.Message, 0, true);
            }
        }

        public static async Task<bool> ConnectAndWaitAsync(string roomId, int timeoutMs = 15000)
        {
            Bootstrap();
            try
            {
                await instance.EnsureConnectedAsync();
                if (instance.IsAlreadyInRequestedRoom(roomId))
                    return true;

                instance.currentMatch = await WithTimeout(instance.socket.JoinMatchAsync(roomId), timeoutMs);
                instance.RebuildPlayersFromMatchPresences();
                return instance.currentMatch != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NakamaTournament] ConnectAndWaitAsync failed: " + ex.Message);
                return false;
            }
        }

        public static void StopMaintainingConnection()
        {
            if (!instance) return;
            _ = instance.StopInternalAsync();
        }

        /// <summary>
        /// Relays the server-decided result to the Nakama match (opcode 13). The runtime broadcasts
        /// MATCH_FINISHED once every player has reported, so the opponent ends instantly. Safe no-op
        /// when Nakama realtime is disabled or the socket is not connected.
        /// </summary>
        public static void ReportFinish(int rank, int score, int prize, int? walletBalance)
        {
            if (instance == null) return;
            _ = instance.ReportFinishAsync(rank, score, prize, walletBalance);
        }

        private async Task ReportFinishAsync(int rank, int score, int prize, int? walletBalance)
        {
            try
            {
                if (socket == null || !socket.IsConnected || currentMatch == null)
                    return;

                var payload = new Dictionary<string, object>
                {
                    { "rank", rank },
                    { "score", score },
                    { "prize", prize },
                };
                if (walletBalance.HasValue)
                    payload["wallet_balance"] = walletBalance.Value;

                string json = JsonConvert.SerializeObject(payload);
                await socket.SendMatchStateAsync(currentMatch.Id, OpReportFinish, Encoding.UTF8.GetBytes(json));
                Debug.Log($"[NakamaTournament] Reported finish rank={rank} score={score} prize={prize}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NakamaTournament] ReportFinish failed: " + ex.Message);
            }
        }

        private bool IsAlreadyInRequestedRoom(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || currentMatch == null)
                return false;

            if (currentMatch.Id == roomId)
                return true;

            return businessRoom != null && businessRoom.roomId == roomId;
        }

        private async Task StopInternalAsync()
        {
            try
            {
                if (socket != null && socket.IsConnected && currentMatch != null)
                    await socket.LeaveMatchAsync(currentMatch);
            }
            catch
            {
                // ignored
            }

            try
            {
                if (socket != null && socket.IsConnected)
                    await socket.CloseAsync();
            }
            catch
            {
                // ignored
            }

            UnhookSocketHandlers();
            currentMatch = null;
            businessRoom = null;
            lastKnownStatus = "waiting";
            playersByNakamaUserId.Clear();
        }

        private async Task EnsureConnectedAsync()
        {
            if (socket != null && socket.IsConnected && session != null && !session.IsExpired)
                return;

            if (client == null)
            {
                // Config-driven so Nakama can point at a real hosted server in production instead of
                // a hardcoded localhost (which only works on the dev machine).
                ApiConfig cfg = ApiConfig.Current;
                client = new Client(
                    cfg.nakamaScheme,
                    cfg.nakamaHost,
                    cfg.nakamaPort,
                    cfg.nakamaServerKey);
                Debug.Log(
                    $"[NakamaTournament] Client -> {cfg.nakamaScheme}://{cfg.nakamaHost}:{cfg.nakamaPort}");
            }

            string deviceId = BuildDeviceId();
            session = await client.AuthenticateDeviceAsync(deviceId, create: true);

            ISocket previousSocket = socket;
            socket = client.NewSocket();
            await socket.ConnectAsync(session, appearOnline: true);

            if (previousSocket != null && previousSocket != socket)
                UnhookSocketHandlers();

            HookSocketHandlers();
        }

        private string BuildDeviceId()
        {
            if (NetworkManager.HasInstance)
            {
                if (!string.IsNullOrEmpty(NetworkManager.Instance.UserUuid))
                    return "app-" + NetworkManager.Instance.UserUuid;
                if (NetworkManager.Instance.UserId > 0)
                    return "app-id-" + NetworkManager.Instance.UserId;
            }
            return "app-device-" + SystemInfo.deviceUniqueIdentifier;
        }

        private void HookSocketHandlers()
        {
            if (socket == null || hookedSocket == socket)
                return;

            UnhookSocketHandlers();
            hookedSocket = socket;
            socket.ReceivedMatchPresence += OnMatchPresence;
            socket.ReceivedMatchState += OnMatchState;
            socket.Closed += OnSocketClosed;
        }

        private void UnhookSocketHandlers()
        {
            if (hookedSocket == null)
                return;

            hookedSocket.ReceivedMatchPresence -= OnMatchPresence;
            hookedSocket.ReceivedMatchState -= OnMatchState;
            hookedSocket.Closed -= OnSocketClosed;
            hookedSocket = null;
        }

        private void OnSocketClosed()
        {
            Debug.LogWarning("[NakamaTournament] Socket closed.");
            if (ApiConfig.Current.UseNakamaRealtimeNetworking && TournamentSession.IsActive)
                _ = TryReconnectAsync();
        }

        private async Task TryReconnectAsync()
        {
            try
            {
                string matchId = currentMatch?.Id;
                await EnsureConnectedAsync();
                if (!string.IsNullOrEmpty(matchId))
                {
                    currentMatch = await socket.JoinMatchAsync(matchId);
                    RebuildPlayersFromMatchPresences();
                    EmitRoomUpdated(BuildRoomFromCurrentState(
                        TournamentSession.Tournament,
                        lastKnownStatus,
                        businessRoom?.startCountdownSeconds,
                        businessRoom?.matchStartAtMs));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NakamaTournament] Reconnect failed: " + ex.Message);
            }
        }

        private string ResolvePresenceStatus()
        {
            if (!string.IsNullOrEmpty(lastKnownStatus))
                return lastKnownStatus;

            string bridgeStatus = TournamentApiBridge.CurrentRoom?.status;
            return string.IsNullOrEmpty(bridgeStatus) ? "waiting" : bridgeStatus;
        }

        private void OnMatchPresence(IMatchPresenceEvent evt)
        {
            if (evt == null || currentMatch == null) return;

            string status = ResolvePresenceStatus();

            if (evt.Joins != null)
            {
                foreach (IUserPresence join in evt.Joins)
                {
                    playersByNakamaUserId[join.UserId] = BuildPlayerDto(join);
                    EmitEvent("player_joined", BuildRoomFromCurrentState(TournamentSession.Tournament, status), null);
                }
            }

            if (evt.Leaves != null)
            {
                foreach (IUserPresence leave in evt.Leaves)
                {
                    playersByNakamaUserId.Remove(leave.UserId);
                    EmitEvent("player_left", BuildRoomFromCurrentState(TournamentSession.Tournament, status), null);
                }
            }

            EmitRoomUpdated(BuildRoomFromCurrentState(TournamentSession.Tournament, status));
        }

        private void OnMatchState(IMatchState matchState)
        {
            if (matchState == null || TournamentSession.Tournament == null)
                return;

            try
            {
                string json = Encoding.UTF8.GetString(matchState.State);
                if (string.IsNullOrEmpty(json))
                    return;

                if (matchState.OpCode == OpRoomState)
                {
                    var state = JsonConvert.DeserializeObject<NakamaRoomStateMessage>(json);
                    if (state == null) return;
                    ApplyIncomingPlayers(state.players);
                    lastKnownStatus = state.status ?? lastKnownStatus;
                    RoomResponseDto room = BuildRoomFromState(state);
                    if (state.status == "starting")
                        EmitEvent("countdown", room);
                    else
                        EmitEvent("room_updated", room);
                    return;
                }

                if (matchState.OpCode == OpMatchStart)
                {
                    var state = JsonConvert.DeserializeObject<NakamaRoomStateMessage>(json);
                    if (state == null) return;
                    ApplyIncomingPlayers(state.players);
                    lastKnownStatus = state.status ?? "active";
                    RoomResponseDto room = BuildRoomFromState(state);
                    EmitEvent("match_start", room);
                    return;
                }

                if (matchState.OpCode == OpMatchFinished)
                {
                    lastKnownStatus = "finished";
                    EmitEvent("match_finished", null, json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NakamaTournament] State parse failed: " + ex.Message);
            }
        }

        private RoomResponseDto BuildRoomFromState(NakamaRoomStateMessage state)
        {
            RoomResponseDto room = BuildRoomFromCurrentState(
                TournamentSession.Tournament,
                state.status ?? lastKnownStatus,
                state.start_countdown_seconds,
                state.match_start_at_ms);
            room.serverNowMs = state.server_now_ms > 0 ? state.server_now_ms : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            room.searchStatus = state.search_status;
            return room;
        }

        private RoomResponseDto BuildRoomFromCurrentState(
            TournamentDefinition tournament,
            string status,
            int? startCountdownSeconds = null,
            long? matchStartAtMs = null)
        {
            tournament ??= TournamentSession.Tournament;
            int maxPlayers = tournament != null ? tournament.maxPlayers : MatchmakerMinPlayersDefault;
            string roomId = businessRoom != null && !string.IsNullOrEmpty(businessRoom.roomId)
                ? businessRoom.roomId
                : currentMatch != null
                    ? currentMatch.Id
                    : string.Empty;

            return new RoomResponseDto
            {
                roomId = roomId,
                tournamentId = businessRoom?.tournamentId ?? (tournament != null ? tournament.id : string.Empty),
                tournamentName = businessRoom?.tournamentName ?? (tournament != null ? tournament.displayName : "Tournament"),
                levelIndex = businessRoom?.levelIndex ?? (TournamentSession.MatchLevelIndex >= 0 ? TournamentSession.MatchLevelIndex : 0),
                levelSeed = businessRoom?.levelSeed ?? TournamentSession.RoomSeed,
                status = status,
                playerCount = Mathf.Max(1, playersByNakamaUserId.Count),
                maxPlayers = maxPlayers,
                waitingSeconds = businessRoom?.waitingSeconds ?? (tournament != null ? tournament.waitingSeconds : 30),
                waitingSecondsRemaining = businessRoom?.waitingSecondsRemaining,
                startCountdownSeconds = startCountdownSeconds ?? businessRoom?.startCountdownSeconds,
                matchStartAtMs = matchStartAtMs ?? businessRoom?.matchStartAtMs,
                serverNowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                searchStatus = status == "starting" ? "match_found" : status == "active" ? "starting" : "searching",
                walletBalance = businessRoom?.walletBalance,
                players = playersByNakamaUserId.Values.ToList()
            };
        }

        private void RebuildPlayersFromMatchPresences()
        {
            playersByNakamaUserId.Clear();
            if (currentMatch == null || currentMatch.Presences == null)
                return;

            foreach (IUserPresence presence in currentMatch.Presences)
                playersByNakamaUserId[presence.UserId] = BuildPlayerDto(presence);
        }

        private RoomPlayerDto BuildPlayerDto(IUserPresence presence)
        {
            bool local = session != null && presence.UserId == session.UserId;

            int localUserId = NetworkManager.HasInstance ? NetworkManager.Instance.UserId : 0;
            int userId = local ? localUserId : StableIdFromString(presence.UserId);
            string display = string.IsNullOrEmpty(presence.Username) ? ("Player " + userId) : presence.Username;

            return new RoomPlayerDto
            {
                userId = userId,
                userUuid = presence.UserId,
                username = display,
                displayName = display,
                isConnected = true,
                hasSubmitted = false
            };
        }

        private void ApplyIncomingPlayers(List<NakamaRoomPlayer> incomingPlayers)
        {
            if (incomingPlayers == null)
                return;

            playersByNakamaUserId.Clear();
            foreach (NakamaRoomPlayer p in incomingPlayers)
            {
                if (p == null || string.IsNullOrEmpty(p.user_uuid))
                    continue;

                playersByNakamaUserId[p.user_uuid] = new RoomPlayerDto
                {
                    userId = p.user_id,
                    userUuid = p.user_uuid,
                    username = p.display_name,
                    displayName = p.display_name,
                    isConnected = p.is_connected,
                    hasSubmitted = false
                };
            }
        }

        private static int StableIdFromString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                    hash = (hash * 31) + value[i];
                return Math.Abs(hash);
            }
        }

        private void EmitRoomUpdated(RoomResponseDto room) => EmitEvent("room_updated", room);

        private void EmitEvent(string eventName, RoomResponseDto room = null, string rawResults = null)
        {
            var payload = new Dictionary<string, object> { { "event", eventName } };
            if (room != null)
                payload["room"] = room;
            if (!string.IsNullOrEmpty(rawResults))
                payload["results"] = JsonConvert.DeserializeObject(rawResults);

            string json = JsonConvert.SerializeObject(payload);
            // Marshal to the Unity main thread (drained in Update) — Nakama raises socket
            // callbacks on a background thread and consumers call Unity APIs.
            mainThreadEvents.Enqueue(json);
        }

        private static async Task<T> WithTimeout<T>(Task<T> task, int timeoutMs)
        {
            Task delay = Task.Delay(timeoutMs);
            Task finished = await Task.WhenAny(task, delay);
            if (finished != task)
                throw new TimeoutException("Nakama realtime timeout.");
            return await task;
        }

        [Serializable]
        private class NakamaRoomStateMessage
        {
            public string status;
            public int? start_countdown_seconds;
            public long? match_start_at_ms;
            public long server_now_ms;
            public string search_status;
            public List<NakamaRoomPlayer> players;
        }

        [Serializable]
        private class NakamaRoomPlayer
        {
            public int user_id;
            public string user_uuid;
            public string display_name;
            public bool is_connected;
        }
    }
}
