using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mkey.Shell;

namespace Mkey
{
    /// <summary>
    /// When the React Native shell owns the match, suppress every Unity gameplay HUD visual so the
    /// RN overlay (Leave | LEVEL | SCORE | TIME | MATCHES | MENU + Shuffle/Hint/Undo) is the only
    /// chrome on screen. Gameplay systems and timer calculation keep running — we only mute
    /// Graphics / CanvasGroups / raycasts, we do not strip board logic.
    /// </summary>
    [DefaultExecutionOrder(320)]
    public class MatchIQShellHudHider : MonoBehaviour
    {
        /// <summary>Exact GameObject names that are pure HUD chrome (scene + runtime).</summary>
        private static readonly string[] HideExact =
        {
            // Top nav (HeaderGUIController children + runtime Leave)
            "LeaveNowButton",
            "ButtonMenu",
            "Level",
            "ScoreCounter",
            "PossibleMatchesCounter",
            "CampaignTimerHud",
            // Bottom boosters + wooden tray
            "ShuffleButton",
            "HintButton",
            "UndoButton",
            "FooterPanel",
            "FooterGui",
            "LayerButtonsPanel",
            "LevelButtons",
        };

        /// <summary>Name contains (case-insensitive) — catch renamed / nested HUD chrome.</summary>
        private static readonly string[] HideNameContains =
        {
            "LeaveNow",
            "ButtonMenu",
            "ScoreCounter",
            "PossibleMatches",
            "CampaignTimer",
            "FooterPanel",
            "FooterGui",
            "LayerButtons",
            "ShuffleButton",
            "HintButton",
            "UndoButton",
        };

        private static readonly HashSet<int> Tagged = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<MatchIQShellHudHider>()) return;
            var host = new GameObject(nameof(MatchIQShellHudHider));
            DontDestroyOnLoad(host);
            host.AddComponent<MatchIQShellHudHider>();
        }

        private void Start() => StartCoroutine(ApplyLoop());

        private IEnumerator ApplyLoop()
        {
            // Aggressive for the first seconds while Leave / themer / timer HUD spawn late.
            float t = 0f;
            while (t < 8f)
            {
                Apply();
                yield return new WaitForSecondsRealtime(0.15f);
                t += 0.15f;
            }

            while (true)
            {
                Apply();
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        private void LateUpdate()
        {
            // Catch same-frame re-enables from themer / header polish (cheap exact-name pass).
            if (!MatchIQShellBridge.IsActive) return;
            for (int i = 0; i < HideExact.Length; i++)
                HideAllNamed(HideExact[i]);
        }

        private static void Apply()
        {
            if (!MatchIQShellBridge.IsActive) return;

            for (int i = 0; i < HideExact.Length; i++)
                HideAllNamed(HideExact[i]);

            // Sweep Transforms in loaded scenes for fuzzy name matches (inactive included).
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (!t || !t.gameObject.scene.IsValid()) continue;
                if (!t.gameObject.scene.isLoaded) continue;
                string n = t.name;
                if (string.IsNullOrEmpty(n)) continue;

                // Exact "Level" is common — only hide under header-like parents.
                if (n == "Level")
                {
                    if (IsHeaderHudContext(t)) HideVisuals(t.gameObject);
                    continue;
                }

                for (int i = 0; i < HideNameContains.Length; i++)
                {
                    if (n.IndexOf(HideNameContains[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        HideVisuals(t.gameObject);
                        break;
                    }
                }
            }

            SuppressUnityPopups();

            // Score / matches binders may re-enable Text each tick — mute their host visuals.
            foreach (var scoreGui in FindObjectsByType<ScoreGUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (scoreGui) HideVisuals(scoreGui.gameObject);
            }
            foreach (var matchesGui in FindObjectsByType<PossibleMathesGUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (matchesGui) HideVisuals(matchesGui.gameObject);
            }
            foreach (var footerGui in FindObjectsByType<FooterGUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (footerGui) HideVisuals(footerGui.gameObject);
            }
        }

        private static bool IsHeaderHudContext(Transform t)
        {
            Transform p = t.parent;
            while (p)
            {
                string n = p.name;
                if (n.IndexOf("Header", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("GUI", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Canvas", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                p = p.parent;
            }
            return false;
        }

        private static void HideAllNamed(string name)
        {
            // Active + inactive objects in loaded scenes.
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (!t || t.name != name) continue;
                if (!t.gameObject.scene.IsValid() || !t.gameObject.scene.isLoaded) continue;
                HideVisuals(t.gameObject);
            }
        }

        /// <summary>
        /// Mute draw + input without SetActive(false), so MonoBehaviours (HintHelper, score
        /// binders, timer expiry) keep updating for shell bridge commands / game logic.
        /// </summary>
        internal static void HideVisuals(GameObject go)
        {
            if (!go) return;

            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (!cg) cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            // Do NOT set ignoreParentGroups — it can leave child raycasts fighting the parent mute.

            Graphic[] graphics = go.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (!graphics[i]) continue;
                graphics[i].enabled = false;
                graphics[i].raycastTarget = false;
            }

            Selectable[] selectables = go.GetComponentsInChildren<Selectable>(true);
            for (int i = 0; i < selectables.Length; i++)
            {
                if (!selectables[i]) continue;
                // Keep component enabled so helpers can still run; only block interaction.
                selectables[i].interactable = false;
            }

            int id = go.GetInstanceID();
            if (Tagged.Add(id))
                Debug.Log("[MatchIQShellHud] Hidden visuals: " + go.name);
        }

        private static void SuppressUnityPopups()
        {
            foreach (var win in FindObjectsByType<PopUpsController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!win) continue;
                string n = win.gameObject.name;
                if (n.IndexOf("Settings", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Message", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("WinPU", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Victory", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Defeat", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Lose", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("NoMatches", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("GetFree", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    HideVisuals(win.gameObject);
                    win.gameObject.SetActive(false);
                }
            }
        }
    }
}
