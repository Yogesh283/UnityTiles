using UnityEngine;
using System;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace Mkey
{
    public class Achievement : MonoBehaviour
    {
        [SerializeField]
        private int targetCount;
        [SerializeField]
        private PopUpsController  rewardPrefabPU;
        [SerializeField]
        private AchievementsLine achievementsLinePrefab;
        [SerializeField]
        private bool resetAfterGetReward;
        [SerializeField]
        private readonly bool dLog;

        #region default
        private string prefix = "achievement_";
        private string SaveStageName { get { return prefix + "stage_" + GetUniqueName(); } }
        private string SaveCountName { get { return prefix + "count_" + GetUniqueName(); } }
        private string SaveRewardReceivedName { get { return prefix + "received_" + GetUniqueName(); } }
        #endregion default

        #region properties
        public bool TargetAchieved { get { return CurrentCount >= TargetCount; } }
        public PopUpsController RewardPUPrefab { get { return rewardPrefabPU; } }
        public int TargetCount { get { return targetCount; } }
        public int CurrentCount { get; private set; }
        public int CurrentStage { get; private set; }
        public bool RewardReceived { get; private set; }
        #endregion properties

        #region events
        public Action <int, int> ChangeCurrentCountEvent;
        public Action<int> ChangeCurrentStageEvent;
        public Action RewardReceivedEvent;
        public Action ResetReceivedEvent;
        #endregion events

        #region reward
        protected void LoadRewardReceived()
        {
            Debug.Log("load SaveRewardReceivedName: " + SaveRewardReceivedName);
            RewardReceived = (PlayerPrefs.GetInt(SaveRewardReceivedName, 0) == 1);
        }

        protected void SetRewardReceived()
        {
            RewardReceived = true;
            PlayerPrefs.SetInt(SaveRewardReceivedName, 1);
        }

        protected void ResetRewardReceived()
        {
            Debug.Log("Reset reward received");
            RewardReceived = false;
            PlayerPrefs.SetInt(SaveRewardReceivedName, 0);
            ResetReceivedEvent?.Invoke();
        }

        public void OnGetRewardEvent()
        {
            if (!TargetAchieved) return;
            SetRewardReceived();
            if(rewardPrefabPU) GuiController.Instance.ShowPopUp(rewardPrefabPU);
            RewardReceivedEvent?.Invoke();
            if (resetAfterGetReward)
            {
                ResetCurrentCount();
                ResetRewardReceived();
            }
            IncCurrentStage();
        }
        #endregion reward

        #region current achievement count
        protected void LoadCurrentCount()
        {
            Debug.Log("Load SaveCountName: " + SaveCountName);
            CurrentCount = PlayerPrefs.GetInt(SaveCountName, 0);
        }

        protected void ResetCurrentCount()
        {
            if (CurrentCount == 0) return;
            CurrentCount = 0;
            PlayerPrefs.SetInt(SaveCountName, CurrentCount);
            ChangeCurrentCountEvent?.Invoke(CurrentCount, targetCount);
            Debug.Log("Reset current count");
        }

        protected void IncCurrentCount()
        {
            CurrentCount++;
            CurrentCount = Mathf.Min(CurrentCount, TargetCount);
            ChangeCurrentCountEvent?.Invoke(CurrentCount, targetCount);
            if(dLog)  Debug.Log(GetUniqueName() + " target " + CurrentCount);
            PlayerPrefs.SetInt(SaveCountName, CurrentCount);
        }
        #endregion current achievement count

        #region current stage
        protected void LoadCurrentStage()
        {
            Debug.Log("Load Stage: " + SaveStageName);
            CurrentStage = PlayerPrefs.GetInt(SaveStageName, 0);
        }

        protected void ResetCurrentStage()
        {
            if (CurrentStage == 0) return;
            CurrentStage = 0;
            PlayerPrefs.SetInt(SaveStageName, CurrentStage);
            ChangeCurrentStageEvent?.Invoke(CurrentStage);
            Debug.Log("Reset current stage");
        }

        protected void IncCurrentStage()
        {
            CurrentStage++;
            ChangeCurrentStageEvent?.Invoke(CurrentStage);
            if (dLog) Debug.Log(GetUniqueName() + " stage " + CurrentStage);
            PlayerPrefs.SetInt(SaveStageName, CurrentStage);
        }
        #endregion current stage

        public AchievementsLine CreateGuiLine(RectTransform parent)
        {
            return (achievementsLinePrefab) ? achievementsLinePrefab.CreateInstance(parent, this) : null;
        }

        public virtual string GetUniqueName() { return "achievement"; }

        public virtual void Load()
        {

        }

        public virtual Sprite GetAchImage()
        {
            return null;
        }

        public virtual void ResetGame()
        {
            ResetCurrentCount();
            ResetRewardReceived();
            ResetCurrentStage();
        }

#if UNITY_EDITOR
        public void DrawInspector()
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            #region test
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.BeginHorizontal("box");

                if (GUILayout.Button("Inc Current Count"))
                {
                    IncCurrentCount();
                }
                if (GUILayout.Button("Reset Current Count"))
                {
                    ResetCurrentCount();
                    ResetRewardReceived();
                }
                if (GUILayout.Button("Reset reward received"))
                {
                    ResetRewardReceived();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Goto play mode for test");
            }
            #endregion test
        }
#endif
    }
}