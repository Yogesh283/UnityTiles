using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Structured tournament multiplayer flow logging (device logcat / Unity console).
    /// </summary>
    public static class TournamentFlowLog
    {
        private const string Tag = "[Tournament]";

        public static void JoinRequest(string detail) => Debug.Log($"{Tag} JOIN_REQUEST {detail}");
        public static void JoinStart(string detail) => Debug.Log($"{Tag} JOIN_START {detail}");
        public static void Countdown3() => Debug.Log($"{Tag} COUNTDOWN_3");
        public static void Countdown2() => Debug.Log($"{Tag} COUNTDOWN_2");
        public static void Countdown1() => Debug.Log($"{Tag} COUNTDOWN_1");
        public static void WsReconnected(string detail) => Debug.Log($"{Tag} WS_RECONNECTED {detail}");
        public static void Join(string message) => Debug.Log($"{Tag} JOIN {message}");
        public static void JoinSuccess(string detail) => Debug.Log($"{Tag} JOIN_SUCCESS {detail}");
        public static void JoinFailed(string detail) => Debug.LogWarning($"{Tag} JOIN_FAILED {detail}");
        public static void RoomJoined(string detail) => Debug.Log($"{Tag} ROOM_JOINED {detail}");
        public static void CountdownStart(string detail) => Debug.Log($"{Tag} COUNTDOWN_START {detail}");
        public static void LevelLoaded(string detail) => Debug.Log($"{Tag} LEVEL_LOADED {detail}");
        public static void PlayerFinished(string detail) => Debug.Log($"{Tag} PLAYER_FINISHED {detail}");
        public static void WinnerSelected(string detail) => Debug.Log($"{Tag} WINNER_SELECTED {detail}");
        public static void LoserSelected(string detail) => Debug.Log($"{Tag} LOSER_SELECTED {detail}");
        public static void PopupOpened(string detail) => Debug.Log($"{Tag} POPUP_OPENED {detail}");
        public static void ReturnToTournament(string detail) => Debug.Log($"{Tag} RETURN_TO_TOURNAMENT {detail}");
        public static void WsConnected(string detail)
        {
            Debug.Log($"{Tag} WS_CONNECTED {detail}");
            Debug.Log($"[WS_CONNECTED] {detail}");
        }
        public static void WsDisconnected(string detail) => Debug.LogWarning($"{Tag} WS_DISCONNECTED {detail}");
        public static void ApiTimeout(string detail) => Debug.LogWarning($"{Tag} API_TIMEOUT {detail}");
        public static void JoinStarted(string tournamentId) =>
            Debug.Log($"{Tag} JOIN_STARTED tournament={tournamentId}");
        public static void WaitingRoomOpened(string tournamentId) =>
            Debug.Log($"{Tag} WAITING_ROOM_OPENED tournament={tournamentId}");
        public static void Searching(string detail) => Debug.Log($"{Tag} SEARCHING {detail}");
        public static void ApiRetry(string detail) => Debug.Log($"{Tag} API_RETRY {detail}");
        public static void PlayerFound(string detail)
        {
            Debug.Log($"{Tag} PLAYER_FOUND {detail}");
            Debug.Log($"[PLAYER_FOUND] {detail}");
        }
        public static void RoomFull(string detail) => Debug.Log($"{Tag} ROOM_FULL {detail}");
        public static void CountdownStarted(string detail)
        {
            Debug.Log($"{Tag} COUNTDOWN_STARTED {detail}");
            Debug.Log($"[COUNTDOWN_STARTED] {detail}");
        }
        public static void GameStarted(string detail) => Debug.Log($"{Tag} GAME_STARTED {detail}");
        public static void GameStart(string detail)
        {
            Debug.Log($"{Tag} GAME_START {detail}");
            Debug.Log($"[GAME_START] {detail}");
        }
        public static void RefundCompleted(string detail) => Debug.Log($"{Tag} REFUND_COMPLETED {detail}");
        public static void RoomClosed(string detail) => Debug.Log($"{Tag} ROOM_CLOSED {detail}");
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
        public static void RoomCreated(string roomId) => Debug.Log($"{Tag} ROOM_CREATED room_id={roomId}");
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
        public static void WaitingState(string phase, string detail) =>
            Debug.Log($"{Tag} WAITING STATE {phase} {detail}");
        public static void WalletSynced(int balance) => Debug.Log($"{Tag} WALLET SYNC balance={balance}");

        public static void WaitingRoomOpen(string detail) => Debug.Log($"[WAITING_ROOM_OPEN] {detail}");
        public static void JoinApiStarted(string detail) => Debug.Log($"[JOIN_API_STARTED] {detail}");
        public static void JoinApiSuccess(string detail) => Debug.Log($"[JOIN_API_SUCCESS] {detail}");
        public static void JoinApiFailed(string detail) => Debug.LogWarning($"[JOIN_API_FAILED] {detail}");
        public static void WsConnecting(string detail) => Debug.Log($"[WS_CONNECTING] {detail}");
        public static void RoomUpdated(string detail) => Debug.Log($"[ROOM_UPDATED] {detail}");
    }
}
