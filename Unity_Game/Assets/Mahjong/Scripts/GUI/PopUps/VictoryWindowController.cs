using UnityEngine;
using UnityEngine.UI;
using System;

namespace Mkey
{
    public class VictoryWindowController : PopUpsController
    {
        [Space(8)]
        [SerializeField]
        private Text NextLevelNumber;
        [SerializeField]
        private Text ScoreCount;
        [SerializeField]
        private Text greetingText;

        [SerializeField]
        private string exceptional = "EXCEPTIONAL!";
        [SerializeField]
        private string good = "GOOD!";

        #region temp
        private TweenSeq ts;
        private int oldCount;
        private GameBoard MBoard => GameBoard.Instance;
        private GameConstructSet GCSet => GameConstructSet.Instance;
        private LevelConstructSet LCSet => GCSet.GetLevelConstructSet(GameLevelHolder.CurrentLevel); 
        private GameObjectsSet GOSet => GCSet.GOSet;
        private GuiController MGui => GuiController.Instance;
        private ScoreHolder MScore => ScoreHolder.Instance;
        private GameLevelHolder MGLevel => GameLevelHolder.Instance;
        #endregion temp

        #region regular
        private void Start()
        {
             // MScore.ChangeEvent.AddListener(SetScore);
        }

        private void OnDestroy()
        {
             // MScore.ChangeEvent.RemoveListener(SetScore);

            if (ts != null) ts.Break();
            SimpleTween.Cancel(gameObject, false);
        }
        #endregion regular

        public override void RefreshWindow()
        {
            SetScore(ScoreHolder.Count, ScoreHolder.AverageScore);
            if (NextLevelNumber) NextLevelNumber.text = "Level " + (GameLevelHolder.CurrentLevel + 2).ToString();
            base.RefreshWindow();
        }

        public void Map_Click()
        {
            CloseWindow();
            ShowStory(()=> 
            {
                SceneLoader.Instance.LoadScene(1);
            });
            
        }

        public void Next_Click()
        {
            CloseWindow();
            ShowStory(() =>
            {
                GameLevelHolder.CurrentLevel++;
                SceneLoader.Instance.ReLoadCurrentScene(true);
            });
        }

        private void SetScore(int score)
        {
            if (ScoreCount)
            {
                int newCount = score;
                SimpleTween.Cancel(ScoreCount.gameObject, false);
                SimpleTween.Value(ScoreCount.gameObject, oldCount, newCount, 0.5f).SetOnUpdate((float val) =>
                {
                    oldCount = (int)val;
                    SetTextString(ScoreCount, oldCount.ToString());
                });
            }
        }

        private void SetScore(int score, int maxScore)
        {
            if (ScoreCount)
            {
                if (score > maxScore) score = maxScore;
                float perc =  (float)score / (float)maxScore * 100f;
                string percS = perc.ToString("0.0");
                SetTextString(ScoreCount, score.ToString() + " (" + percS + "%)");

                if (greetingText) greetingText.text = (perc < 100f) ? good : exceptional;
            }
        }

        private void ShowStory (Action completeCallBack)
        {
            if (LCSet.LevelWinStoryPage && MGui)
            {
                MGui.ShowPopUp(LCSet.LevelWinStoryPage, completeCallBack);
            }

            else completeCallBack?.Invoke();
        }
    }
}