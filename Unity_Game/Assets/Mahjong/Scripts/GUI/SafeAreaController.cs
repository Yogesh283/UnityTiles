using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mkey
{
    /// <summary>
    /// Installs safe-area handling for the gameplay scene. Enforces Canvas Scaler
    /// (Scale With Screen Size · 1080×2400 · match 0.5) on the gameplay HUD canvas and
    /// re-applies it when the screen or safe area changes.
    ///
    /// Per-widget layout (header / leave / board / boosters) reads <see cref="WxoSafeArea"/>
    /// directly so everything clears the Android status bar and gesture / nav bar.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SafeAreaController : MonoBehaviour
    {
        private int lastW = -1;
        private int lastH = -1;
        private Rect lastSafe;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryInstall(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryInstall(scene);

        private static void TryInstall(Scene scene)
        {
            if (scene.buildIndex != Tournament.TournamentSession.GameSceneIndex) return;
            if (FindFirstObjectByType<SafeAreaController>()) return;
            new GameObject(nameof(SafeAreaController)).AddComponent<SafeAreaController>();
        }

        private void Start() => StartCoroutine(ConfigureWhenReady());

        private IEnumerator ConfigureWhenReady()
        {
            yield return null;
            yield return null;
            Configure();
        }

        private void Configure()
        {
            Canvas hud = ResolveHudCanvas();
            if (hud) WxoSafeArea.ConfigureScaler(hud);
            lastW = Screen.width;
            lastH = Screen.height;
            lastSafe = Screen.safeArea;
        }

        private static Canvas ResolveHudCanvas()
        {
            if (HeaderGUIController.Instance)
            {
                Canvas c = HeaderGUIController.Instance.GetComponentInParent<Canvas>();
                if (c) return c.rootCanvas;
            }
            GameObject canvasMain = GameObject.Find("CanvasMain");
            if (canvasMain)
            {
                Canvas c = canvasMain.GetComponent<Canvas>();
                if (c) return c.rootCanvas;
            }
            return null;
        }

        private void Update()
        {
            if (Screen.width == lastW && Screen.height == lastH && Screen.safeArea.Equals(lastSafe))
                return;
            Configure();
        }
    }
}
