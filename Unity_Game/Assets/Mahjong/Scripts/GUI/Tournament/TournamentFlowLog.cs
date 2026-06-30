using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Structured tournament multiplayer flow logging (device logcat / Unity console).
    /// </summary>
    public static class TournamentFlowLog
    {
        private const string Tag = "[TournamentFlow]";

        public static void Join(string message) => Debug.Log($"{Tag} JOIN {message}");
        public static void JoinResponseParsed(
            string roomId,
            string status,
            int playerCount,
            long? matchStartAtMs) =>
            Debug.Log(
                $"{Tag} JOIN RESPONSE PARSED room_id={roomId} status={status} " +
                $"playerCount={playerCount} match_start_at_ms={matchStartAtMs?.ToString() ?? "null"}");
        public static void ConnectingWebSocket(string roomId) =>
            Debug.Log($"{Tag} CONNECTING WEBSOCKET room_id={roomId}");
        public static void RoomCreated(string roomId) => Debug.Log($"{Tag} ROOM CREATED room_id={roomId}");
        public static void RoomId(string roomId) => Debug.Log($"{Tag} ROOM ID {roomId}");
        public static void PlayerJoined(string message) => Debug.Log($"{Tag} PLAYER JOINED {message}");
        public static void WebSocketConnected(string roomId) => Debug.Log($"{Tag} WEBSOCKET CONNECTED room={roomId}");
        public static void WebSocketDisconnected(string roomId, string reason) =>
            Debug.LogWarning($"{Tag} WEBSOCKET DISCONNECTED room={roomId} reason={reason}");
        public static void WebSocketReconnecting(string roomId, int attempt) =>
            Debug.Log($"{Tag} WEBSOCKET RECONNECTING room={roomId} attempt={attempt}");
        public static void Countdown(string message) => Debug.Log($"{Tag} COUNTDOWN {message}");
        public static void MatchStart(string message) => Debug.Log($"{Tag} MATCH START {message}");
        public static void MatchFinished(string message) => Debug.Log($"{Tag} MATCH FINISHED {message}");
        public static void Winner(string message) => Debug.Log($"{Tag} WINNER {message}");
        public static void Loser(string message) => Debug.Log($"{Tag} LOSER {message}");
        public static void BoardFrozen(string reason) => Debug.Log($"{Tag} BOARD FROZEN {reason}");
        public static void BoardUnfrozen(string reason) => Debug.Log($"{Tag} BOARD UNFROZEN {reason}");
        public static void SubmitScore(string message) => Debug.Log($"{Tag} SUBMIT SCORE {message}");
        public static void SubmitScoreError(string message) => Debug.LogError($"{Tag} SUBMIT SCORE ERROR {message}");
        public static void RoomDestroyed(string roomId) => Debug.Log($"{Tag} ROOM DESTROYED room={roomId}");
        public static void Event(string eventName, string detail) => Debug.Log($"{Tag} WS EVENT {eventName} {detail}");
    }
}
