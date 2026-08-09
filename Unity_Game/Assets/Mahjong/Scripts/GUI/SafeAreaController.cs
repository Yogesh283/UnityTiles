using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mkey
{
    /// <summary>
    /// Installs safe-area handling for the gameplay scene. Its job is to enforce the required Canvas
    /// Scaler config (Scale With Screen Size · 1080×1920 · match 0.5) on the gameplay HUD canvas and
    /// re-apply it if the screen changes (rotation / fold).
    ///
    /// The actual downward shift of the HUD, timer badge and board is applied by
    /// <see cref="CampaignTimerHud"/>, <see cref="BoardScreenFitter"/> and <see cref="GameHudThemer"/>,
    /// all of which read <see cref="WxoSafeArea"/> directly. This controller only owns the scaler so
    /// those consumers compute their insets against the correct px-per-unit.
    ///
    /// Runtime-only (created via <see cref="RuntimeInitializeOnLoadMethod"/> like the other themers),
    /// so deleting this file fully reverts the behaviour. It only ever touches the HUD canvas that
    /// hosts the header — never the level-constructor canvas.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SafeAreaController : MonoBehaviour
    {
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
            // The HUD canvas builds over the first couple of frames.
            yield return null;
            yield return null;
            Configure();
        }

        private void Configure()
        {
            Canvas hud = ResolveHudCanvas();
            if (hud) WxoSafeArea.ConfigureScaler(hud);
        }

        /// <summary>The gameplay HUD canvas (the one hosting the header) — never the constructor.</summary>
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

        private int lastW = -1;
        private int lastH = -1;

        private void Update()
        {
            if (Screen.width == lastW && Screen.height == lastH) return;
            lastW = Screen.width;
            lastH = Screen.height;
            Configure();
        }
    }
}
