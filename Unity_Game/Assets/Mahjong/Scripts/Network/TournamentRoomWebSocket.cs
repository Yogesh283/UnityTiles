using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mkey.Tournament;
using UnityEngine;

namespace Mkey.Network
{
    /// <summary>
    /// Real-time tournament room WebSocket client (server-authoritative room events).
    /// </summary>
    public class TournamentRoomWebSocket : MonoBehaviour
    {
        private static TournamentRoomWebSocket instance;

        private ClientWebSocket socket;
        private CancellationTokenSource cancelSource;
        private readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();
        private string activeRoomId;
        private bool maintainConnection;
        private int reconnectAttempts;

        public static bool IsConnected =>
            instance != null && instance.socket != null && instance.socket.State == WebSocketState.Open;

        public static string ActiveRoomId => instance != null ? instance.activeRoomId : null;

        public static event Action<string> MessageReceived;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance) return;
            GameObject host = new GameObject(nameof(TournamentRoomWebSocket));
            instance = host.AddComponent<TournamentRoomWebSocket>();
            DontDestroyOnLoad(host);
        }

        private void Update()
        {
            while (incoming.TryDequeue(out string message))
                MessageReceived?.Invoke(message);
        }

        private void OnDestroy()
        {
            StopMaintainingConnection();
        }

        public static void Connect(string roomId)
        {
            Bootstrap();
            _ = instance.ConnectAndWaitInternalAsync(roomId, 15000);
        }

        public static Task<bool> ConnectAndWaitAsync(string roomId, int timeoutMs = 15000)
        {
            Bootstrap();
            return instance.ConnectAndWaitInternalAsync(roomId, timeoutMs);
        }

        public static void Disconnect() => StopMaintainingConnection();

        public static void StopMaintainingConnection()
        {
            if (!instance) return;
            instance.maintainConnection = false;
            string roomId = instance.activeRoomId;
            instance.DisconnectSocket("client_disconnect");
            instance.activeRoomId = null;
            if (!string.IsNullOrEmpty(roomId))
                TournamentFlowLog.RoomDestroyed(roomId);
        }

        private async Task<bool> ConnectAndWaitInternalAsync(string roomId, int timeoutMs)
        {
            if (string.IsNullOrEmpty(roomId) || !NetworkManager.HasInstance || !NetworkManager.Instance.IsAuthenticated)
            {
                Debug.LogWarning("[TournamentWS] connect skipped — missing room id or auth.");
                return false;
            }

            maintainConnection = true;
            activeRoomId = roomId;
            reconnectAttempts = 0;

            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            try
            {
                return await ConnectWithRetriesAsync(roomId, timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[TournamentWS] connect timed out room={roomId}");
                return IsConnected;
            }
        }

        private async Task<bool> ConnectWithRetriesAsync(string roomId, CancellationToken outerToken)
        {
            const int maxAttempts = 5;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                outerToken.ThrowIfCancellationRequested();

                if (await TryConnectOnceAsync(roomId, attempt))
                    return true;

                if (attempt < maxAttempts)
                    await Task.Delay(attempt * 400, outerToken);
            }

            return false;
        }

        private async Task<bool> TryConnectOnceAsync(string roomId, int attempt)
        {
            DisconnectSocket("reconnect");

            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsAuthenticated)
                return false;

            socket = new ClientWebSocket();
            cancelSource = new CancellationTokenSource();

            string token = Uri.EscapeDataString(NetworkManager.Instance.AccessToken);
            string url = ApiConfig.Current.WebSocketRoot + "/" + roomId + "?token=" + token;

            try
            {
                Debug.Log($"[TournamentWS] connecting ({attempt}) room={roomId} url={ApiConfig.Current.WebSocketRoot}");
                await socket.ConnectAsync(new Uri(url), cancelSource.Token);

                if (socket.State != WebSocketState.Open)
                    return false;

                reconnectAttempts = 0;
                TournamentFlowLog.WebSocketConnected(roomId);
                _ = ReceiveLoop();
                _ = PingLoop();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TournamentWS] connect failed ({attempt}): {ex.Message}");
                DisconnectSocket("connect_failed");
                return false;
            }
        }

        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[8192];
            StringBuilder builder = new StringBuilder();
            string roomId = activeRoomId;

            while (socket != null && socket.State == WebSocketState.Open && cancelSource != null)
            {
                try
                {
                    builder.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancelSource.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            HandleDisconnect(roomId, "server_close");
                            return;
                        }

                        builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    if (builder.Length > 0)
                        incoming.Enqueue(builder.ToString());
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[TournamentWS] receive error: " + ex.Message);
                    HandleDisconnect(roomId, ex.Message);
                    return;
                }
            }
        }

        private void HandleDisconnect(string roomId, string reason)
        {
            TournamentFlowLog.WebSocketDisconnected(roomId ?? activeRoomId, reason);
            DisconnectSocket(reason);

            if (!maintainConnection || string.IsNullOrEmpty(activeRoomId) || !TournamentSession.IsActive)
                return;

            _ = ReconnectAfterDelayAsync(activeRoomId);
        }

        private async Task ReconnectAfterDelayAsync(string roomId)
        {
            if (!maintainConnection || !TournamentSession.IsActive)
                return;

            reconnectAttempts++;
            if (reconnectAttempts > 8)
            {
                Debug.LogWarning("[TournamentWS] max reconnect attempts reached");
                return;
            }

            TournamentFlowLog.WebSocketReconnecting(roomId, reconnectAttempts);
            await Task.Delay(Mathf.Min(5000, reconnectAttempts * 500));

            if (!maintainConnection || !TournamentSession.IsActive || IsConnected)
                return;

            await TryConnectOnceAsync(roomId, reconnectAttempts);
        }

        private async Task PingLoop()
        {
            while (socket != null && socket.State == WebSocketState.Open && cancelSource != null)
            {
                try
                {
                    await Task.Delay(15000, cancelSource.Token);
                    await SendJson("{\"event\":\"ping\"}");
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    return;
                }
            }
        }

        private async Task SendJson(string json)
        {
            if (socket == null || socket.State != WebSocketState.Open || cancelSource == null)
                return;

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancelSource.Token);
        }

        private void DisconnectSocket(string reason)
        {
            cancelSource?.Cancel();
            cancelSource?.Dispose();
            cancelSource = null;

            if (socket != null)
            {
                try
                {
                    if (socket.State == WebSocketState.Open)
                        socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
                }
                catch
                {
                    // ignored
                }

                socket.Dispose();
                socket = null;
            }
        }
    }
}
