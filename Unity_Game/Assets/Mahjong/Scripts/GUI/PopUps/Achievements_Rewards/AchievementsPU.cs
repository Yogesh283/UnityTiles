using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class AchievementsPU : PopUpsController
	{
        [SerializeField]
        private RectTransform LinesParent;
        [SerializeField]
        private GameObject scrollFlag;
        private List<AchievementsLine> achievementsLines;

        #region temp vars

        #endregion temp vars


        #region regular
        public override void RefreshWindow()
        {
            CreateAchievTab();
            base.RefreshWindow();
        }

        private void CreateAchievTab()
        {
            AchievementsLine[] sT = LinesParent.GetComponentsInChildren<AchievementsLine>();
            foreach (var item in sT)
            {
                DestroyImmediate(item.gameObject);
            }

            AchievementsController p = AchievementsController.Instance;
            if (p == null) return;

            List<Achievement> products = p.achievements;

            achievementsLines = new List<AchievementsLine>();
            foreach (var item in products)
            {
                if (item)
                {
                    AchievementsLine aL = item.CreateGuiLine(LinesParent);
                    if (aL) { achievementsLines.Add(aL);  aL.GetClickAction += CloseWindow; }
                };
            }

            if (scrollFlag) scrollFlag.SetActive(achievementsLines.Count > 5);
        }
        #endregion regular
    }
}
