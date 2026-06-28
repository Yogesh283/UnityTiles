using UnityEngine;
using UnityEngine.Events;
using System;
using System.Globalization;
using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace Mkey
{
    [CreateAssetMenu(menuName = "ScriptableObjects/ScoreHolder")]
    public class ScoreHolder : SingletonScriptableObject<ScoreHolder>
    {
        #region default data
        [Space(10, order = 0)]
        [Header("Default data", order = 1)]

        [SerializeField]
        private bool saveBestResult = true;
        #endregion default data

        #region keys
        [SerializeField]
        private string saveKey = "mk_score";
        #endregion keys

        #region temp vars
        private static bool loaded = false;
        private static int _count;
        private static List<int> levelsScore;  // temporary array for store scores
        #endregion temp vars

        public static int AverageScore { get; private set; }

        public static int Count
        {
            get { if (!loaded) Instance.Load(); return _count; }
            private set { _count = value; }
        }
        public static IList<int> LevelsScoreStore => levelsScore.AsReadOnly(); 

        public UnityEvent<int> ChangeEvent;
        public UnityEvent<List<int>> LoadEvent;

        private void Awake()
        {
            Load();
            Debug.Log("Awake: " + this + " ;count: " + Count);
        }

        public static void Add(int count)
        {
            if (Instance)
            {
                Instance.SetCount(Count + count);
            }
        }

        /// <summary>
        /// Set count and save result
        /// </summary>
        /// <param name="count"></param>
        public void SetCount(int count)
        {
            count = Mathf.Max(0, count);
            bool changed = (Count != count);
            Count = count;
            if (changed)
            {
                ChangeEvent?.Invoke(Count);
            }
        }

        /// <summary>
        /// Load serialized data or set defaults
        /// </summary>
        public void Load()
        {
            levelsScore = new List<int>();
            loaded = true;
            Debug.Log("scoreholder:" + PlayerPrefs.GetString(saveKey, "none"));
            ListWrapperStruct<int> lW = PlayerPrefsExtension.GetObject<ListWrapperStruct<int>>(saveKey, new ListWrapperStruct<int>(levelsScore));
            LoadEvent?.Invoke(lW.list);
        }

        /// <summary>
        /// save scores after passing the game level
        /// </summary>
        /// <param name="passedLevel"></param>
        public void Save(int passedLevel)
        {
            if (levelsScore == null) levelsScore = new List<int>();
            int count = levelsScore.Count;
            if (count <= passedLevel) // increase scores list
            {
                levelsScore.AddRange(new int[passedLevel - count + 1]);
            }

            int score = (saveBestResult) ? Mathf.Max(Count, levelsScore[passedLevel]) : Count;
            levelsScore[passedLevel] = score;
           
            ListWrapperStruct <int> lW = new ListWrapperStruct <int> (levelsScore);
            PlayerPrefsExtension.SetObject<ListWrapperStruct<int>>(saveKey, lW);
        }

        public void SetDefaultData()
        {
            PlayerPrefs.DeleteKey(saveKey);
            SetCount(0);
        }

        public void SetAverageScore(int averageScore)
        {
            AverageScore = Mathf.Max(1, averageScore); // no zero
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ScoreHolder))]
    public class ScoreHolderEditor : Editor
    {
        private bool test = true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            ScoreHolder tH = (ScoreHolder)target;
            EditorGUILayout.LabelField("Score: " + ScoreHolder.Count);

            #region test
            if (test = EditorGUILayout.Foldout(test, "Test"))
            {
                EditorGUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Add score"))
                {
                    ScoreHolder.Add(100);
                }

                if (GUILayout.Button("Dec score"))
                {
                    ScoreHolder.Add(-100);
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");

                if (GUILayout.Button("Reset to default"))
                {
                    tH.SetDefaultData();
                }

                if (GUILayout.Button("Log count"))
                {
                    Debug.Log("Score: " + ScoreHolder.Count);

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