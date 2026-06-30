using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Mkey
{
    /// <summary>
    /// Keeps exactly one EventSystem alive across scene loads.
    /// </summary>
    public static class UiEventSystemGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnforceSingle();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnforceSingle();
        }

        public static void EnforceSingle()
        {
            EventSystem[] systems = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (systems.Length == 0)
            {
                CreatePersistentEventSystem();
                return;
            }

            if (systems.Length == 1)
                return;

            EventSystem keep = ChoosePrimary(systems);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] == keep)
                    continue;

                Object.Destroy(systems[i].gameObject);
            }
        }

        private static EventSystem ChoosePrimary(EventSystem[] systems)
        {
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] && systems[i].gameObject.scene.name == "DontDestroyOnLoad")
                    return systems[i];
            }

            if (EventSystem.current)
                return EventSystem.current;

            return systems[0];
        }

        private static void CreatePersistentEventSystem()
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(go);
        }
    }
}
