using UnityEngine;

namespace Mkey.Network
{
    /// <summary>
    /// Single configuration source for backend URLs and development mode.
    /// Assign via Resources/Network/ApiConfig or use defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "TilesClash/Api Config")]
    public class ApiConfig : ScriptableObject
    {
        [Header("Backend URLs")]
        [Tooltip("Local FastAPI server (XAMPP development)")]
        public string baseUrl = "http://localhost:8000";

        [Tooltip("Production API")]
        public string productionUrl = "https://api.matchiq.fun";

        public bool useProductionUrl;

        [Header("Mode")]
        [Tooltip("When enabled, all systems use local simulation (no API calls).")]
        public bool developmentMode;

        [Header("Request")]
        public float requestTimeoutSeconds = 15f;

        private static ApiConfig _instance;

        public static ApiConfig Current
        {
            get
            {
                if (_instance) return _instance;
                _instance = Resources.Load<ApiConfig>("Network/ApiConfig");
                if (!_instance)
                {
                    _instance = CreateInstance<ApiConfig>();
                    Debug.LogWarning("[ApiConfig] Resources/Network/ApiConfig not found — using runtime defaults.");
                }
                return _instance;
            }
        }

        public string ServerRoot => (useProductionUrl ? productionUrl : baseUrl).TrimEnd('/');

        public string ApiV1Root => ServerRoot + "/api/v1";

        public string WebSocketRoot => ServerRoot.Replace("https://", "wss://").Replace("http://", "ws://") + "/ws/tournament";

        public bool UseLocalSimulation => developmentMode;
    }
}
