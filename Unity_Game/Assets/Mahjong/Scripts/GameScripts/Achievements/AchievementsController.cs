using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class AchievementsController : MonoBehaviour
	{
        public List<Achievement> achievements;

        public bool HaveTargetAchieved { get; private set; }

        public Action<bool> HaveTargetAchievedEvent;

        #region temp vars
        private GameConstructSet GCSet { get { return GameConstructSet.Instance; } }
        private LevelConstructSet LCSet { get { return GCSet.GetLevelConstructSet(GameLevelHolder.CurrentLevel); } }
        private GameObjectsSet GOSet { get { return GCSet.GOSet; } }
        #endregion temp vars

        public static AchievementsController Instance;
		
		#region regular
		private void Start()
		{
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            foreach (var item in achievements)
            {
                item.Load();
                item.ChangeCurrentCountEvent += (c, t) => { CheckState(); };
                item.RewardReceivedEvent += () => { CheckState(); };
            }
            CheckState();
        }
		#endregion regular

        private void CheckState()
        {
            bool temp = HaveTargetAchieved;
            HaveTargetAchieved = false;
            foreach (var item in achievements)
            {
                if (item.TargetAchieved && !item.RewardReceived) 
                {
                    HaveTargetAchieved = true;
                    break;
                }
            }

           // if (temp != HaveTargetAchieved)
                HaveTargetAchievedEvent?.Invoke(HaveTargetAchieved);
        }
	}
}
