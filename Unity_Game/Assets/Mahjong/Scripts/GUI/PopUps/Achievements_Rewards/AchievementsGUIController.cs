using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class AchievementsGUIController : MonoBehaviour
	{
        [SerializeField]
        private Image actionFlagImage;
        [SerializeField]
        private PopUpsController achievementsPUPrefab;

        #region temp vars
        private GuiController MGui { get { return GuiController.Instance; } }
        private AchievementsController AC { get { return AchievementsController.Instance; } }
        #endregion temp vars

        #region regular
        private IEnumerator Start()
		{
            while (!AC) yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            AC.HaveTargetAchievedEvent += Refresh;
            Refresh(AC.HaveTargetAchieved);
        }
		
		private void OnDestroy()
        {
            if (AC) AC.HaveTargetAchievedEvent -= Refresh;
        }
		#endregion regular

        public void Button_Click()
        {
            MGui.ShowPopUp(achievementsPUPrefab);
        }

        private void Refresh(bool haveAchievement)
        {
            if (actionFlagImage) actionFlagImage.enabled = haveAchievement;
        }
	}
}
