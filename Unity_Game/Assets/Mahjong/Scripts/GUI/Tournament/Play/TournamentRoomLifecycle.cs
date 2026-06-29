using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Keeps waiting rooms and matches ticking — anti-freeze watchdog, disconnect recovery.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class TournamentRoomLifecycle : MonoBehaviour
    {
        private static TournamentRoomLifecycle instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance) return;
            GameObject host = new GameObject(nameof(TournamentRoomLifecycle));
            instance = host.AddComponent<TournamentRoomLifecycle>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            TournamentRoomRegistry.TickWaitingRoom(dt);
            TournamentRoomRegistry.TickActiveMatch(dt);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                TournamentRoomRegistry.NotifyLocalDisconnect();
            else
                TournamentRoomRegistry.TryReconnectLocal();
        }

    }
}
