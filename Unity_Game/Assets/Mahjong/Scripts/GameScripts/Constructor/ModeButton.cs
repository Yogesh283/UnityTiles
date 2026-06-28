using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class ModeButton : MonoBehaviour
	{
        [SerializeField]
        private Button modeButton;
        [SerializeField]
        private Text modeText;
        #region temp vars

        #endregion temp vars
        private GameConstructSet GCSet { get { return GameConstructSet.Instance; } }

        #region regular

        private void Start()
		{
#if UNITY_EDITOR
            if (modeButton)
            {
                modeButton.gameObject.SetActive(true);
                if(modeText)  modeText.text = (GameBoard.GMode == GameMode.Edit) ? "To Play Mode" : "To Edit Mode";
                modeButton.onClick.AddListener(() =>
                {
                    if (GameBoard.GMode == GameMode.Edit)
                    {
                        GameBoard.GMode = GameMode.Play;
                        if (modeText) modeText.text = "To Edit Mode";
                    }
                    else
                    {
                        SimpleTween.ForceCancelAll();
                        GameBoard.GMode = GameMode.Edit;
                        if (modeText) modeText.text = "To Play Mode";
                    }
                    SceneLoader.Instance.ReLoadCurrentScene(false);
                });
            }
#else
           if (modeButton) modeButton.gameObject.SetActive(false); 
#endif
        }

        public void NextLevel()
        {
            if (!GCSet) return;
            int levelCount = GCSet.LevelCount;
            if (GameLevelHolder.CurrentLevel < levelCount - 1)
            {
                GameLevelHolder.CurrentLevel++;
                SceneLoader.Instance.ReLoadCurrentScene(false);
            }
        }

        public void PrevLevel()
        {
            if (!GCSet) return;
            if (GameLevelHolder.CurrentLevel > 0)
            {
                GameLevelHolder.CurrentLevel--;
                SceneLoader.Instance.ReLoadCurrentScene(false);
            }
        }
        #endregion regular
    }
}
