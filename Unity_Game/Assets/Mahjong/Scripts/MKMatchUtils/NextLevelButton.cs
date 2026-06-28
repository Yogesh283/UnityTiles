using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    public class NextLevelButton : MonoBehaviour
    {
        [SerializeField]
        private int sceneNumber = 1;
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
            GameLevelHolder.CurrentLevel  = nextLevel;
           // if (LifesHolder.Count <= 0 && !GCSet.UnLimited) { MGui.ShowMessage("Sorry!", "You have no lifes.", 1.5f, () => { MGui.ShowPopUpByDescription("lifeshop"); }); return; }
            Debug.Log("load scene : " + sceneNumber+ " ;CurrentLevel: " + GameLevelHolder.CurrentLevel);
            // GameBoard.showMission = true;
            if (SceneLoader.Instance) SceneLoader.Instance.LoadScene(sceneNumber);
        }
    }
}