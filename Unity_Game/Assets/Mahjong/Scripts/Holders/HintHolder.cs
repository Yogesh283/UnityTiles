using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace Mkey
{
    [CreateAssetMenu(menuName = "ScriptableObjects/HintHolder")]
    public class HintHolder : SingletonScriptableObject<HintHolder>
    {
        #region default data
        [Space(10, order = 0)]
        [Header("Default data", order = 1)]
        [Tooltip("Default count at start")]
        [SerializeField]
        private int defCount = 5;
        #endregion default data

        #region keys
        [SerializeField]
        private string saveKey = "mk_mahjong_hints"; // current hints
        #endregion keys

        #region temp vars
        private static bool loaded = false;
        private static int _count;
        #endregion temp vars

        public static int Count
        {
            get { if (!loaded) Instance.Load(); return _count; }
            private set { _count = value; }
        }
        public int DefaultCount => defCount;

        public UnityEvent<int> ChangeEvent;
        public UnityEvent<int> LoadEvent;

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
        /// Set coins and save result
        /// </summary>
        /// <param name="count"></param>
        public void SetCount(int count)
        {
            count = Mathf.Max(0, count);
            bool changed = (Count != count);
            Count = count;
            if (changed)
            {
                PlayerPrefs.SetInt(saveKey, Count);
            }
            if (changed) ChangeEvent?.Invoke(Count);
        }

        /// <summary>
        /// Load serialized hints or set defaults
        /// </summary>
        public void Load()
        {
            loaded = true;
            Count = PlayerPrefs.GetInt(saveKey, defCount);
            LoadEvent?.Invoke(Count);
        } 

        public void SetDefaultData()
        {
            SetCount(defCount);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(HintHolder))]
    public class HintHolderEditor : Editor
    {
        private bool test = true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            HintHolder cH = (HintHolder)target;
            EditorGUILayout.LabelField("Count: " + HintHolder.Count);

            if (test = EditorGUILayout.Foldout(test, "Test"))
            {
                EditorGUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Add 5 hints"))
                {
                    HintHolder.Add(5);
                }
                if (GUILayout.Button("Set 5 hints"))
                {
                    cH.SetCount(5);
                }
                if (GUILayout.Button("Clear hints"))
                {
                    cH.SetCount(0);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Reset to default"))
                {
                    cH.SetDefaultData();
                }
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Log hints"))
                {
                    Debug.Log("Hints: " + HintHolder.Count);

                }
                if (GUILayout.Button("Load saved hints"))
                {
                    cH.Load();
                }
            }
        }
    }
#endif
}