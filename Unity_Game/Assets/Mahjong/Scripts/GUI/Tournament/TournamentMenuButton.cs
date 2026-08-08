using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mkey.Tournament
{
    public class TournamentMenuButton : MonoBehaviour
    {
        [SerializeField] private int tournamentSceneIndex = 3;

        public void Click()
        {
            // Tournament selection lives in React Native now; the Unity scene is no longer built.
            if (tournamentSceneIndex < 0 || tournamentSceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Shell.MatchIQShellBridge.ReturnToShell(false);
                return;
            }

            // Instant load — skip SceneLoader progress animation (~1s+ delay).
            SceneManager.LoadScene(tournamentSceneIndex);
        }
    }
}
