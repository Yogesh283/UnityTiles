using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
        private static bool nakamaHooked;

        private ClientWebSocket socket;
        private CancellationTokenSource cancelSource;
        private readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();
        private readonly SemaphoreSlim connectLock = new SemaphoreSlim(1, 1);
        private Task<bool> pendingConnectTask;
        private string pendingConnectRoomId;
        private string activeRoomId;
        private bool maintainConnection;
        private int reconnectAttempts;
        private bool isReconnecting;

        /// <summary>Last connect failure propagated to callers (e.g. join coordinator catch).</summary>
        public static Exception LastConnectException { get; private set; }

        public static bool IsConnected =>
            instance != null && instance.socket != null && instance.socket.State == WebSocketState.Open;

        public static bool IsReconnecting =>
            instance != null && instance.isReconnecting;

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
            if (ApiConfig.Current.UseNakamaRealtimeNetworking)
            {
                HookNakamaMessageBridge();
                _ = NakamaTournamentRealtimeClient.ConnectAndWaitAsync(roomId, 15000);
                return;
            }

            Bootstrap();
            TournamentWebSocketProbe.ConnectRequested(
                roomId, "Connect()", instance != null && instance.socket?.State == WebSocketState.Open);
            _ = instance.ConnectAndWaitInternalAsync(roomId, 15000);
        }

        public static Task<bool> ConnectAndWaitAsync(string roomId, int timeoutMs = 15000)
        {
            if (ApiConfig.Current.UseNakamaRealtimeNetworking)
            {
                HookNakamaMessageBridge();
                return NakamaTournamentRealtimeClient.ConnectAndWaitAsync(roomId, timeoutMs);
            }

            Bootstrap();
            TournamentWebSocketProbe.ConnectRequested(
                roomId, "ConnectAndWaitAsync()", instance != null && instance.socket?.State == WebSocketState.Open);
            return instance.ConnectAndWaitInternalAsync(roomId, timeoutMs);
        }

        public static void Disconnect() => StopMaintainingConnection();

        public static void StopMaintainingConnection()
        {
            if (ApiConfig.Current.UseNakamaRealtimeNetworking)
            {
                NakamaTournamentRealtimeClient.StopMaintainingConnection();
                return;
            }

            if (!instance) return;
            instance.maintainConnection = false;
            instance.isReconnecting = false;
            string roomId = instance.activeRoomId;
            instance.DisconnectSocket("client_disconnect");
            instance.activeRoomId = null;
            if (!string.IsNullOrEmpty(roomId))
                TournamentFlowLog.RoomDestroyed(roomId);
        }

        /// <summary>Bridges Nakama adapter events into the same pipeline as FastAPI WebSocket.</summary>
        public static void EnsureNakamaMessageBridge()
        {
            if (nakamaHooked)
                return;

            nakamaHooked = true;
            NakamaTournamentRealtimeClient.MessageReceived += json => MessageReceived?.Invoke(json);
        }

        private static void HookNakamaMessageBridge() => EnsureNakamaMessageBridge();

        private async Task<bool> ConnectAndWaitInternalAsync(string roomId, int timeoutMs)
        {
            LastConnectException = null;

            if (string.IsNullOrEmpty(roomId))
                throw new InvalidOperationException("[TournamentWS] connect aborted — roomId is null or empty.");

            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsAuthenticated)
                throw new InvalidOperationException("[TournamentWS] connect aborted — NetworkManager missing or not authenticated.");

            if (IsConnected && activeRoomId == roomId)
            {
                maintainConnection = true;
                return true;
            }

            if (pendingConnectTask != null && !pendingConnectTask.IsCompleted && pendingConnectRoomId == roomId)
                return await pendingConnectTask;

            await connectLock.WaitAsync();
            try
            {
                if (IsConnected && activeRoomId == roomId)
                {
                    maintainConnection = true;
                    return true;
                }

                if (pendingConnectTask != null && !pendingConnectTask.IsCompleted && pendingConnectRoomId == roomId)
                    return await pendingConnectTask;

                maintainConnection = true;
                activeRoomId = roomId;
                reconnectAttempts = 0;
                pendingConnectRoomId = roomId;
                pendingConnectTask = ConnectWithTimeoutAsync(roomId, timeoutMs);
                return await pendingConnectTask;
            }
            finally
            {
                pendingConnectTask = null;
                pendingConnectRoomId = null;
                connectLock.Release();
            }
        }

        private async Task<bool> ConnectWithTimeoutAsync(string roomId, int timeoutMs)
        {
            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token);
            try
            {
                return await ConnectWithRetriesAsync(roomId, linked.Token);
            }
            catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested)
            {
                var timeoutEx = new TimeoutException(
                    $"[TournamentWS] connect timed out after {timeoutMs}ms roomId={roomId} lastState={DescribeSocketState(socket)}",
                    ex);
                LogConnectFailure("connect timeout", timeoutEx, roomId, null, WebSocketState.None, socket?.State);
                LastConnectException = timeoutEx;
                throw timeoutEx;
            }
        }

        private async Task<bool> ConnectWithRetriesAsync(string roomId, CancellationToken outerToken)
        {
            const int maxAttempts = 5;
            Exception lastError = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                outerToken.ThrowIfCancellationRequested();

                try
                {
                    if (await TryConnectOnceAsync(roomId, attempt))
                        return true;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    LastConnectException = ex;
                }

                if (attempt < maxAttempts)
                    await Task.Delay(attempt * 400, outerToken);
            }

            if (lastError != null)
                throw lastError;

            var noOpen = new InvalidOperationException(
                $"[TournamentWS] connect failed after {maxAttempts} attempts — socket never reached Open. roomId={roomId}");
            LogConnectFailure("connect exhausted", noOpen, roomId, null, WebSocketState.None, socket?.State);
            LastConnectException = noOpen;
            throw noOpen;
        }

        private async Task<bool> TryConnectOnceAsync(string roomId, int attempt)
        {
            if (IsConnected && activeRoomId == roomId)
                return true;

            DisconnectSocket("reconnect");

            if (!NetworkManager.HasInstance || !NetworkManager.Instance.IsAuthenticated)
            {
                throw new InvalidOperationException(
                    "[TournamentWS] connect aborted — NetworkManager missing or not authenticated.");
            }

            socket = new ClientWebSocket();
            cancelSource = new CancellationTokenSource();

            WebSocketState stateBefore = socket.State;
            (string url, string urlForLog, int jwtLength) = BuildConnectUrl(roomId);

            Debug.Log(
                "[TournamentWS] connect attempt\n" +
                $"  attempt={attempt}\n" +
                $"  roomId={roomId}\n" +
                $"  jwtLength={jwtLength}\n" +
                $"  url={urlForLog}\n" +
                $"  finalUrl={url}\n" +
                $"  stateBefore={stateBefore}");

            try
            {
                await socket.ConnectAsync(new Uri(url), cancelSource.Token);

                WebSocketState stateAfter = socket.State;
                Debug.Log(
                    $"[TournamentWS] ConnectAsync returned roomId={roomId} stateAfter={stateAfter}");

                if (socket.State != WebSocketState.Open)
                {
                    var stateEx = new InvalidOperationException(
                        $"[TournamentWS] ConnectAsync completed but socket state is {stateAfter}, expected Open. " +
                        $"roomId={roomId} url={urlForLog}");
                    LogConnectFailure("connect bad state", stateEx, roomId, urlForLog, stateBefore, stateAfter);
                    throw stateEx;
                }

                reconnectAttempts = 0;
                isReconnecting = false;
                TournamentFlowLog.WebSocketConnected(roomId);
                TournamentFlowLog.WsConnected($"room={roomId}");
                if (attempt > 1)
                    TournamentFlowLog.WsReconnected($"room={roomId} attempt={attempt}");
                TournamentWebSocketProbe.Connected(roomId, attempt);
                _ = ReceiveLoop();
                _ = PingLoop();
                return true;
            }
            catch (Exception ex)
            {
                WebSocketState stateAfter = socket != null ? socket.State : WebSocketState.Closed;
                LogConnectFailure($"connect failed attempt={attempt}", ex, roomId, urlForLog, stateBefore, stateAfter);
                DisconnectSocket("connect_failed");
                throw;
            }
        }

        private static (string url, string urlForLog, int jwtLength) BuildConnectUrl(string roomId)
        {
            string jwt = NetworkManager.Instance.AccessToken ?? string.Empty;
            int jwtLength = jwt.Length;
            string escaped = Uri.EscapeDataString(jwt);
            string root = ApiConfig.Current.WebSocketRoot.TrimEnd('/');
            string url = root + "/" + roomId + "?token=" + escaped;
            string urlForLog = root + "/" + roomId + "?token=<redacted len=" + jwtLength + ">";
            return (url, urlForLog, jwtLength);
        }

        private static void LogConnectFailure(
            string context,
            Exception ex,
            string roomId,
            string urlForLog,
            WebSocketState stateBefore,
            WebSocketState? stateAfter)
        {
            LastConnectException = ex;

            if (!string.IsNullOrEmpty(urlForLog))
                Debug.LogError($"[TournamentWS] {context} roomId={roomId} url={urlForLog}");
            else
                Debug.LogError($"[TournamentWS] {context} roomId={roomId}");

            Debug.LogError(
                $"[TournamentWS] ClientWebSocket.State before={stateBefore} after={FormatState(stateAfter)}");

            if (TryGetHandshakeHttpStatus(ex, out int httpStatus))
                Debug.LogError($"[TournamentWS] WebSocket handshake HTTP status={httpStatus}");
            else
                Debug.LogError("[TournamentWS] WebSocket handshake HTTP status=unknown");

            Exception inner = ex.InnerException;
            if (inner != null)
            {
                Debug.LogError(
                    $"[TournamentWS] InnerException type={inner.GetType().FullName} message={inner.Message}");
            }
            else
            {
                Debug.LogError("[TournamentWS] InnerException=null");
            }

            Debug.LogException(ex);
        }

        private static string FormatState(WebSocketState? state) =>
            state.HasValue ? state.Value.ToString() : "n/a";

        private static string DescribeSocketState(ClientWebSocket ws) =>
            ws == null ? "null" : ws.State.ToString();

        private static bool TryGetHandshakeHttpStatus(Exception ex, out int statusCode)
        {
            statusCode = 0;
            for (Exception current = ex; current != null; current = current.InnerException)
            {
                if (TryReadHttpStatusProperty(current, out statusCode))
                    return true;

                if (TryParseHttpStatusFromMessage(current.Message, out statusCode))
                    return true;
            }

            return false;
        }

        private static bool TryReadHttpStatusProperty(Exception ex, out int statusCode)
        {
            statusCode = 0;
            PropertyInfo prop = ex.GetType().GetProperty("StatusCode", BindingFlags.Public | BindingFlags.Instance);
            if (prop == null)
                return false;

            object value = prop.GetValue(ex);
            if (value == null)
                return false;

            if (value is int i)
            {
                statusCode = i;
                return statusCode > 0;
            }

            string text = value.ToString();
            return int.TryParse(text, out statusCode) && statusCode > 0;
        }

        private static bool TryParseHttpStatusFromMessage(string message, out int statusCode)
        {
            statusCode = 0;
            if (string.IsNullOrEmpty(message))
                return false;

            Match match = Regex.Match(message, @"\b(401|403|404|426|500|502|503)\b");
            if (!match.Success)
                match = Regex.Match(message, @"HTTP[^\d]*(\d{3})", RegexOptions.IgnoreCase);

            return match.Success && int.TryParse(match.Groups[1].Value, out statusCode);
        }

        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[8192];
            StringBuilder builder = new StringBuilder();
            string roomId = activeRoomId;
            TournamentWebSocketProbe.ReceiveLoopStarted(roomId);

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
                            TournamentWebSocketProbe.ReceiveLoopExited(roomId, "server_close_frame");
                            HandleDisconnect(roomId, "server_close");
                            return;
                        }

                        builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    while (!result.EndOfMessage);

                    if (builder.Length > 0)
                    {
                        string message = builder.ToString();
                        LogIncomingMessage(roomId, message);
                        incoming.Enqueue(message);
                    }
                }
                catch (OperationCanceledException)
                {
                    TournamentWebSocketProbe.ReceiveLoopExited(roomId, "cancelled");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TournamentWS] receive error roomId={roomId}");
                    TournamentWebSocketProbe.ReceiveLoopExited(roomId, ex.Message);
                    TournamentWebSocketProbe.Disconnect(roomId, "receive_exception", ex);
                    if (ex.InnerException != null)
                    {
                        Debug.LogError(
                            $"[TournamentWS] receive InnerException type={ex.InnerException.GetType().FullName} " +
                            $"message={ex.InnerException.Message}");
                    }

                    Debug.LogException(ex);
                    HandleDisconnect(roomId, ex.Message);
                    return;
                }
            }

            TournamentWebSocketProbe.ReceiveLoopExited(roomId, "loop_condition_false");
        }

        private static void LogIncomingMessage(string roomId, string message)
        {
            string eventName = "unknown";
            if (message.Contains("\"pong\""))
            {
                eventName = "pong";
                TournamentWebSocketProbe.PongReceived(roomId);
            }
            else if (message.Contains("\"event\""))
            {
                int i = message.IndexOf("\"event\"", StringComparison.Ordinal);
                if (i >= 0)
                {
                    int colon = message.IndexOf(':', i);
                    int q1 = message.IndexOf('"', colon + 1);
                    int q2 = message.IndexOf('"', q1 + 1);
                    if (q1 >= 0 && q2 > q1)
                        eventName = message.Substring(q1 + 1, q2 - q1 - 1);
                }
            }

            TournamentWebSocketProbe.ServerMessage(roomId, eventName, message.Length);
        }

        private void HandleDisconnect(string roomId, string reason)
        {
            TournamentWebSocketProbe.Disconnect(roomId ?? activeRoomId, reason);
            TournamentFlowLog.WebSocketDisconnected(roomId ?? activeRoomId, reason);
            TournamentFlowLog.WsDisconnected($"room={roomId ?? activeRoomId} reason={reason}");
            DisconnectSocket(reason);

            if (!maintainConnection || string.IsNullOrEmpty(activeRoomId) || !TournamentSession.IsActive)
                return;

            // A reconnect chain is already running — never launch a second one, or two chains race
            // and can leave two live sockets on the same room (duplicate websocket connections).
            if (isReconnecting)
                return;

            isReconnecting = true;
            _ = ReconnectAfterDelayAsync(activeRoomId);
        }

        private async Task ReconnectAfterDelayAsync(string roomId)
        {
            if (!maintainConnection || !TournamentSession.IsActive)
                return;

            reconnectAttempts++;
            isReconnecting = true;

            TournamentFlowLog.WebSocketReconnecting(roomId, reconnectAttempts);
            await Task.Delay(Mathf.Min(5000, reconnectAttempts * 500));

            if (!maintainConnection || !TournamentSession.IsActive || IsConnected)
            {
                isReconnecting = false;
                return;
            }

            try
            {
                await TryConnectOnceAsync(roomId, reconnectAttempts);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TournamentWS] reconnect failed roomId={roomId} attempt={reconnectAttempts}");
                Debug.LogException(ex);
            }

            if (!IsConnected && maintainConnection && TournamentSession.IsActive)
                _ = ReconnectAfterDelayAsync(roomId);
            else
                isReconnecting = false;
        }

        private async Task PingLoop()
        {
            string roomId = activeRoomId;
            TournamentWebSocketProbe.PingLoopStarted(roomId);
            while (socket != null && socket.State == WebSocketState.Open && cancelSource != null)
            {
                try
                {
                    await Task.Delay(15000, cancelSource.Token);
                    TournamentWebSocketProbe.PingSent(roomId);
                    await SendJson("{\"event\":\"ping\"}");
                }
                catch (OperationCanceledException)
                {
                    TournamentWebSocketProbe.PingLoopExited(roomId, "cancelled");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogError("[TournamentWS] ping loop error");
                    TournamentWebSocketProbe.PingLoopExited(roomId, ex.Message);
                    TournamentWebSocketProbe.Disconnect(roomId, "ping_loop_exception", ex);
                    Debug.LogException(ex);
                    return;
                }
            }

            TournamentWebSocketProbe.PingLoopExited(roomId, "loop_condition_false");
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
            TournamentWebSocketProbe.Disconnect(activeRoomId, "DisconnectSocket:" + reason);
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
