using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
    21.08.2024 - LoadNextScene(), LoadPrevScene()
    04.04.2021 - add autoload
    26.01.2021
 */
namespace Mkey
{
    public class SceneLoadHelper : MonoBehaviour
    {
        private SceneLoader SL => SceneLoader.Instance;

        public bool autoLoad = false;
        [ShowIfTrue("autoLoad")]
        public float autoLoadDelay = 0f;
        [ShowIfTrue("autoLoad")]
        public int autoLoadSceneIndex = 0;

        private IEnumerator Start()
        {
            if (autoLoad) 
            {
                yield return new WaitForSeconds(autoLoadDelay);
                LoadSceneByIndex(autoLoadSceneIndex);
            }
        }

        /// <summary>
        /// Load scene by build index. Menu scenes were removed from the build (React Native owns
        /// them now), so anything outside the shipped range hands control back to the shell.
        /// </summary>
        public void LoadSceneByIndex(int scene)
        {
            if (scene < 0 || scene >= SceneManager.sceneCountInBuildSettings)
            {
                Shell.MatchIQShellBridge.ReturnToShell(false);
                return;
            }

            if (SL) SL.LoadScene(scene);
        }

        public void LoadNextScene()
        {
            int currIndex = SceneLoader.GetCurrentSceneBuildIndex();
            int next = currIndex + 1;
            if (next < SceneManager.sceneCountInBuildSettings) LoadSceneByIndex(next);
            else LoadSceneByIndex(0);
        }

        public void LoadPrevScene()
        {
            int currIndex = SceneLoader.GetCurrentSceneBuildIndex();
            int next = currIndex - 1;
            if (next < 0) LoadSceneByIndex(SceneManager.sceneCountInBuildSettings - 1);
            else LoadSceneByIndex(next);
        }
    }
}