using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace Mkey
{
	public class WinLevelAchievement : Achievement
	{
        #region events

        #endregion events

        #region regular
        public override void Load()
        {
            LoadRewardReceived();
            LoadCurrentCount();
            LoadCurrentStage();

            GameEvents.WinLevelAction += WinLevelEventHandler;
            //RewardReceivedEvent +=(r)=> 
            //{
            //    CoinsHolder.Add(r);
            //};

            ChangeCurrentCountEvent += (cc, tc)=>{  };
        }

        private void OnDestroy()
        {
            GameEvents.WinLevelAction -= WinLevelEventHandler;
        }
        #endregion regular

        public override string GetUniqueName()
        {
            return "winlevels";
        }

        private void WinLevelEventHandler()
        {
            IncCurrentCount();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(WinLevelAchievement))]
    public class WinLevelAchievementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WinLevelAchievement t = (WinLevelAchievement)target;
            t.DrawInspector();
        }
    }
#endif
}
