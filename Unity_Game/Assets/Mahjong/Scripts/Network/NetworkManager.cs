using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Mkey.Network
{
    /// <summary>
    /// Singleton HTTP client for all backend API calls.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public const string ServerUnavailableMessage = "Server unavailable.";

        private static NetworkManager _instance;

        public static NetworkManager Instance
        {
            get
            {
                EnsureExists();
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;

        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);
        public string AccessToken { get; private set; }
        public int UserId { get; private set; }

        public event Action<bool> ServerAvailabilityChanged;

        private bool _serverAvailable = true;

        public static void EnsureExists()
        {
            if (_instance) return;
            var go = new GameObject(nameof(NetworkManager));
            _instance = go.AddComponent<NetworkManager>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (_instance && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSession();
        }

        public void LoadSession()
        {
            AccessToken = PlayerPrefs.GetString("mk_api_token", string.Empty);
            UserId = PlayerPrefs.GetInt("mk_user_id", 0);
        }

        public void SaveSession(string token, int userId)
        {
            AccessToken = token ?? string.Empty;
            UserId = userId;
            PlayerPrefs.SetString("mk_api_token", AccessToken);
            PlayerPrefs.SetInt("mk_user_id", UserId);
            PlayerPrefs.Save();
        }

        public void ClearSession()
        {
            AccessToken = string.Empty;
            UserId = 0;
            PlayerPrefs.DeleteKey("mk_api_token");
            PlayerPrefs.DeleteKey("mk_user_id");
            PlayerPrefs.Save();
        }

        public Task<ApiResult<TResponse>> GetAsync<TResponse>(string relativePath, bool requireAuth = true) =>
            SendAsync<TResponse>(relativePath, UnityWebRequest.kHttpVerbGET, null, requireAuth);

        public Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(
            string relativePath,
            TRequest body,
            bool requireAuth = true) =>
            SendAsync<TResponse>(relativePath, UnityWebRequest.kHttpVerbPOST, body, requireAuth);

        public async Task<ApiResult<TResponse>> SendAsync<TResponse>(
            string relativePath,
            string method,
            object body,
            bool requireAuth)
        {
            if (ApiConfig.Current.UseLocalSimulation)
                return ApiResult<TResponse>.Fail("Development mode enabled.");

            if (requireAuth && string.IsNullOrEmpty(AccessToken))
                return ApiResult<TResponse>.Fail("Not authenticated.");

            string url = BuildUrl(relativePath);
            float timeout = Mathf.Max(1f, ApiConfig.Current.requestTimeoutSeconds);

            using var request = new UnityWebRequest(url, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Mathf.CeilToInt(timeout);

            if (body != null)
            {
                string json = JsonConvert.SerializeObject(body);
                byte[] payload = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            if (requireAuth && !string.IsNullOrEmpty(AccessToken))
                request.SetRequestHeader("Authorization", "Bearer " + AccessToken);

            string deviceId = SystemInfo.deviceUniqueIdentifier;
            if (!string.IsNullOrEmpty(deviceId))
                request.SetRequestHeader("X-Device-Id", deviceId);

            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                UnityWebRequestAsyncOperation op = request.SendWebRequest();
                while (!op.isDone)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        request.Abort();
                        SetServerAvailable(false);
                        return ApiResult<TResponse>.Fail(ServerUnavailableMessage, 0, true);
                    }
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    SetServerAvailable(false);
                    return ApiResult<TResponse>.Fail(ServerUnavailableMessage, 0, true);
                }

                SetServerAvailable(true);
                long code = request.responseCode;

                if (code >= 200 && code < 300)
                {
                    if (typeof(TResponse) == typeof(Unit))
                        return ApiResult<TResponse>.Ok(default);

                    if (string.IsNullOrWhiteSpace(request.downloadHandler.text))
                        return ApiResult<TResponse>.Ok(default);

                    TResponse data = JsonConvert.DeserializeObject<TResponse>(request.downloadHandler.text);
                    return ApiResult<TResponse>.Ok(data);
                }

                string detail = ExtractErrorDetail(request.downloadHandler.text);
                bool unavailable = code == 0 || code >= 500;
                if (unavailable) SetServerAvailable(false);
                return ApiResult<TResponse>.Fail(
                    string.IsNullOrEmpty(detail) ? ServerUnavailableMessage : detail,
                    (int)code,
                    unavailable);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NetworkManager] Request failed: " + ex.Message);
                SetServerAvailable(false);
                return ApiResult<TResponse>.Fail(ServerUnavailableMessage, 0, true);
            }
        }

        private static string BuildUrl(string relativePath)
        {
            string root = ApiConfig.Current.ApiV1Root.TrimEnd('/');
            string path = relativePath.TrimStart('/');
            return root + "/" + path;
        }

        private static string ExtractErrorDetail(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                var err = JsonConvert.DeserializeObject<ErrorBody>(body);
                return err?.detail;
            }
            catch
            {
                return body.Length > 120 ? body.Substring(0, 120) : body;
            }
        }

        private void SetServerAvailable(bool available)
        {
            if (_serverAvailable == available) return;
            _serverAvailable = available;
            ServerAvailabilityChanged?.Invoke(available);
        }

        [Serializable]
        private class ErrorBody
        {
            public string detail;
        }

        public struct Unit { }
    }
}
