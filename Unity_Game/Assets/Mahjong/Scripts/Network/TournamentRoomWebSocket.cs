using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        public static bool IsConnected =>
            instance != null && instance.socket != null && instance.socket.State == WebSocketState.Open;

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
            Disconnect();
        }

        public static void Connect(string roomId)
        {
            Bootstrap();
            instance.ConnectInternal(roomId);
        }

        public static void Disconnect()
        {
            if (!instance) return;
            instance.DisconnectInternal();
        }

        private async void ConnectInternal(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || !NetworkManager.HasInstance || !NetworkManager.Instance.IsAuthenticated)
                return;

            if (activeRoomId == roomId && IsConnected)
                return;

            DisconnectInternal();
            activeRoomId = roomId;

            string token = Uri.EscapeDataString(NetworkManager.Instance.AccessToken);
            string url = ApiConfig.Current.WebSocketRoot + "/" + roomId + "?token=" + token;

            socket = new ClientWebSocket();
            cancelSource = new CancellationTokenSource();

            try
            {
                await socket.ConnectAsync(new Uri(url), cancelSource.Token);
                _ = ReceiveLoop();
                _ = PingLoop();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TournamentWS] connect failed: " + ex.Message);
                DisconnectInternal();
            }
        }

        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[8192];
            StringBuilder builder = new StringBuilder();

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
                            DisconnectInternal();
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
                    DisconnectInternal();
                    return;
                }
            }
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

        private void DisconnectInternal()
        {
            cancelSource?.Cancel();
            cancelSource?.Dispose();
            cancelSource = null;

            if (socket != null)
            {
                try
                {
                    if (socket.State == WebSocketState.Open)
                        socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
                catch
                {
                    // ignored
                }

                socket.Dispose();
                socket = null;
            }

            activeRoomId = null;
        }
    }
}
