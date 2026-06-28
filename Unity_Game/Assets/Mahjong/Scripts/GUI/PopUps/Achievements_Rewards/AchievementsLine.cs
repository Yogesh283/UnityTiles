using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

namespace Mkey
{
	public class AchievementsLine : MonoBehaviour
	{
        public Image achImage;
        [SerializeField]
        private Text countText;
        [SerializeField]
        private GameObject completeGroup;
        [SerializeField]
        private Button getButton;

        private Achievement achievement;

        public UnityEvent <float> ChangeProgressEvent;
        public Action GetClickAction;

        #region temp vars

        #endregion temp vars

        #region regular
        private void Start()
        {
            achievement.RewardReceivedEvent += RewardReceivedEventHandler;
            achievement.ChangeCurrentCountEvent += RefreshCount;
            achievement.ResetReceivedEvent += RefreshRewardGroup;
            if(achImage)
            {
                Sprite s =  achievement.GetAchImage();
                if (s)
                {
                    achImage.sprite = s;
                    achImage.gameObject.SetActive(true);
                }
            }
        }
		
		private void OnDestroy()
        {
            if (achievement)
            {
                achievement.RewardReceivedEvent -= RewardReceivedEventHandler;
                achievement.ChangeCurrentCountEvent -= RefreshCount;
                achievement.ResetReceivedEvent -= RefreshRewardGroup;
            }
        }
		#endregion regular

        public AchievementsLine CreateInstance(RectTransform parent, Achievement achievement)
        {
            if (!parent || !achievement) return null;
          
            AchievementsLine achievementsLine = Instantiate(this, parent);
            achievementsLine.transform.localScale = Vector3.one;
            achievementsLine.achievement = achievement;

            achievementsLine.RefreshCount(achievement.CurrentCount, achievement.TargetCount);
            achievementsLine.RefreshRewardGroup();

            return achievementsLine;
        }

        private void RefreshCount(int currentCount, int targetCount)
        {
            if (countText)
            {
                countText.text = currentCount + "/" + targetCount;
            }
            RefreshRewardGroup();
            ChangeProgressEvent?.Invoke((float)currentCount / (float) targetCount);
        }

        private void RefreshRewardGroup()
        {
            if (getButton) getButton.gameObject.SetActive(achievement.TargetAchieved && !achievement.RewardReceived);
            if (completeGroup) completeGroup.SetActive(achievement.RewardReceived);
            // if (rewardCountText) rewardCountText.text = achievement.AchReward.ToString();
        }

        public void GetButton_Click()
        {
            if (achievement.RewardReceived)
            {
                Debug.Log("reward received");
                return;
            }
            if (achievement.TargetAchieved) achievement.OnGetRewardEvent();
            GetClickAction?.Invoke();
        }

        #region event handlers
        private void RewardReceivedEventHandler()
        {
            RefreshRewardGroup();
        }
        #endregion event handlers
    }
}
