using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace Mkey
{
	public class UseShuffleAchievement : Achievement
	{
        #region events

        #endregion events

        #region regular
        public override void Load()
        {
            LoadRewardReceived();
            LoadCurrentCount();
            LoadCurrentStage();

            GameEvents.ApplyShuffleAction += UseShuffleEventHandler;
            ChangeCurrentCountEvent += (cc, tc)=>{  };
        }

        private void OnDestroy()
        {
            GameEvents.ApplyShuffleAction -= UseShuffleEventHandler;
        }
        #endregion regular

        public override string GetUniqueName()
        {
            return "useshuffle";
        }

        private void UseShuffleEventHandler()
        {
            IncCurrentCount();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UseShuffleAchievement))]
    public class UseShuffleAchievementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UseShuffleAchievement t = (UseShuffleAchievement)target;
            t.DrawInspector();
        }
    }
#endif
}
