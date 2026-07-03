using System;
using UnityEngine;

namespace Mkey.Network
{
    /// <summary>
    /// TEMPORARY — WebSocket lifecycle probe. Remove after investigation.
    /// Filter logcat: [TournamentWsProbe]
    /// </summary>
    public static class TournamentWebSocketProbe
    {
        private const string Tag = "[TournamentWsProbe]";

        public static void Connected(string roomId, int attempt)
        {
            Debug.Log($"{Tag} WebSocket CONNECTED room={roomId} attempt={attempt}");
        }

        public static void PingSent(string roomId)
        {
            Debug.Log($"{Tag} Ping sent (app-level JSON) room={roomId}");
        }

        public static void PongReceived(string roomId)
        {
            Debug.Log($"{Tag} Pong received (app-level JSON) room={roomId}");
        }

        public static void ServerMessage(string roomId, string eventName, int bytes)
        {
            Debug.Log($"{Tag} Server message room={roomId} event={eventName} bytes={bytes}");
        }

        public static void ReceiveLoopStarted(string roomId)
        {
            Debug.Log($"{Tag} ReceiveLoop STARTED room={roomId} thread={Environment.CurrentManagedThreadId}");
        }

        public static void ReceiveLoopExited(string roomId, string reason)
        {
            Debug.LogWarning($"{Tag} ReceiveLoop EXITED room={roomId} reason={reason}");
        }

        public static void PingLoopStarted(string roomId)
        {
            Debug.Log($"{Tag} PingLoop STARTED room={roomId}");
        }

        public static void PingLoopExited(string roomId, string reason)
        {
            Debug.LogWarning($"{Tag} PingLoop EXITED room={roomId} reason={reason}");
        }

        public static void Disconnect(string roomId, string reason, Exception ex = null)
        {
            if (ex != null)
            {
                Debug.LogError($"{Tag} Disconnect reason={reason} room={roomId} ex={ex.GetType().Name}: {ex.Message}");
                Debug.LogException(ex);
            }
            else
            {
                Debug.LogWarning($"{Tag} Disconnect reason={reason} room={roomId}");
            }
        }

        public static void ConnectRequested(string roomId, string source, bool alreadyOpen)
        {
            Debug.Log(
                $"{Tag} Connect requested source={source} room={roomId} " +
                $"alreadyOpen={alreadyOpen} maintain={TournamentRoomWebSocket.IsConnected}");
        }

        public static void Step(int n, string name, bool pass, string detail = null)
        {
            string verdict = pass ? "PASS" : "FAIL";
            string msg = string.IsNullOrEmpty(detail)
                ? $"{Tag} STEP {n} {name} => {verdict}"
                : $"{Tag} STEP {n} {name} => {verdict} | {detail}";
            if (pass) Debug.Log(msg);
            else Debug.LogWarning(msg);
        }
    }
}
