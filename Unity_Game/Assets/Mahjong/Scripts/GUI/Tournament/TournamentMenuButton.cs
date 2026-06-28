using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mkey.Tournament
{
    public class TournamentMenuButton : MonoBehaviour
    {
        [SerializeField] private int tournamentSceneIndex = 3;

        public void Click()
        {
            // Instant load — skip SceneLoader progress animation (~1s+ delay).
            SceneManager.LoadScene(tournamentSceneIndex);
        }
    }
}
