using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    public class NextLevelButton : MonoBehaviour
    {
        [SerializeField]
        private Text levelNumber;
        [SerializeField]
        private string prefix = "Level ";

        #region temp vars
        private GuiController MGui { get { return GuiController.Instance; } }
        private GameConstructSet GCSet { get { return GameConstructSet.Instance; } }
        private LevelConstructSet LCSet { get { return GCSet.GetLevelConstructSet(GameLevelHolder.CurrentLevel); } }
        private GameObjectsSet GOSet { get { return GCSet.GOSet; } }
        private int nextLevel = 0;
        private bool loading;
        #endregion temp vars

        #region regular
        private void Start()
        {
            int topPassedLevel = GameLevelHolder.TopPassedLevel;
            nextLevel = topPassedLevel + 1;
            if (levelNumber) levelNumber.text = prefix + (nextLevel+1).ToString();
        }
        #endregion regular

        public void Click()
        {
            // Re-entrancy guard: the button was firing every frame and, with a stale scene index,
            // bouncing off SceneLoader's out-of-range guard in a tight loop.
            if (loading) return;
            loading = true;

            Tournament.TournamentSession.Clear();
            GameLevelHolder.CurrentLevel = nextLevel;

            // Menu scenes were dropped from the build — the gameplay scene is the only shipped one.
            // Advancing a level means reloading it, so always target the real game scene index
            // instead of the legacy serialized value (which still pointed at the old build index 2).
            int target = Tournament.TournamentSession.GameSceneIndex;
            Debug.Log("load scene : " + target + " ;CurrentLevel: " + GameLevelHolder.CurrentLevel);

            if (SceneLoader.Instance) SceneLoader.Instance.LoadScene(target);
            else loading = false;
        }
    }
}