using UnityEngine;
using UnityEngine.Events;
using System;
using System.Globalization;
using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
#endif

/*
  game level holder
  17.06.2021
 */

namespace Mkey
{
    [CreateAssetMenu(menuName = "ScriptableObjects/GameLevelHolder")]
    public class GameLevelHolder : SingletonScriptableObject<GameLevelHolder>
    {
        #region keys
        [SerializeField]
        private string saveKey = "mk_gamelevel";
        #endregion keys

        #region temp vars
        private static bool loaded = false;
        private static int _count;
        private static List<int> levelsStars;  // temporary array for store levels stars
        #endregion temp vars

        public static int TopPassedLevel
        {
            get
            { if (!loaded) Instance.Load(); return _count; }
            private set { _count = value; }
        }

        public static int CurrentLevel;

        public UnityEvent <int> ChangePassedEvent;
        public UnityEvent <int> LoadEvent;
        public UnityEvent <int> PassLevelEvent;
        public UnityEvent <int> StartLevelEvent;

        private void Awake()
        {
            Load();
            Debug.Log("Awake: " + this + " ;top passed level: " + TopPassedLevel);
            CurrentLevel = 0;
        }

        public void Load()
        {
            loaded = true;
            TopPassedLevel = PlayerPrefs.GetInt(saveKey, -1);
            LoadEvent?.Invoke(TopPassedLevel);
        }

        public void Save()
        {
            // save top passed level
            PlayerPrefs.SetInt(saveKey, TopPassedLevel);
        }

        public void SetDefaultData()
        {
            TopPassedLevel = -1;
            PlayerPrefs.DeleteKey(saveKey);
            ChangePassedEvent?.Invoke(TopPassedLevel);
        }

        public void PassLevel()
        {
            if (CurrentLevel > TopPassedLevel)
            {
                TopPassedLevel = CurrentLevel; 
                ChangePassedEvent?.Invoke(TopPassedLevel);
            }

            Save();
            PassLevelEvent?.Invoke(CurrentLevel);
        }

        public static void StartLevel()
        {
            LevelBoosterResetService.OnLevelStart(CurrentLevel);
            if (Instance) Instance.StartLevelEvent?.Invoke(CurrentLevel);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GameLevelHolder))]
    public class GameLevelHolderEditor : Editor
    {
        private bool test = true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GameLevelHolder tH = (GameLevelHolder)target;
            EditorGUILayout.LabelField("Top passed level: " + GameLevelHolder.TopPassedLevel);

            #region test
            if (test = EditorGUILayout.Foldout(test, "Test"))
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Reset to default"))
                {
                    tH.SetDefaultData();
                }

                if (GUILayout.Button("Log"))
                {
                    Debug.Log("Top passed level: " + GameLevelHolder.TopPassedLevel);

                }

                if (GUILayout.Button("Load data"))
                {
                    tH.Load();
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion test
        }
    }
#endif
}