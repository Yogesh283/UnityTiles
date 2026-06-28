using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
    using UnityEditor;
#endif

/*
  player game coins holder
  04.05.2021
  27.05.2021
 */
namespace Mkey
{
    [CreateAssetMenu(menuName = "ScriptableObjects/CoinsHolder")]
    public class CoinsHolder : SingletonScriptableObject<CoinsHolder>
    {
        #region default data
        [Space(10, order = 0)]
        [Header("Default data", order = 1)]
        [Tooltip("Default count at start")]
        [SerializeField]
        private int defCount = 500;

        [Tooltip("Default facebook coins")]
        [SerializeField]
        private int defFBCoinsCount = 100;
        #endregion default data

        #region keys
        [SerializeField]
        private string saveKey = "mk_match_coins"; // current coins
        [SerializeField]
        private string saveFbCoinsKey = "mk_fbcoins"; // facebook coins
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
                if (Mkey.Network.ApiConfig.Current.UseLocalSimulation)
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
            if (changed && Mkey.Network.ApiConfig.Current.UseLocalSimulation)
            {
                PlayerPrefs.SetInt(saveKey, Count);
            }
            if (changed) ChangeEvent?.Invoke(Count);
        }

        /// <summary>
        /// Add facebook gift (only once), and save flag.
        /// </summary>
        public void AddFbCoins()
        {
            if (!PlayerPrefs.HasKey(saveFbCoinsKey) || PlayerPrefs.GetInt(saveFbCoinsKey) == 0)
            {
                PlayerPrefs.SetInt(saveFbCoinsKey, 1);
                Add(defFBCoinsCount);
            }
        }

        /// <summary>
        /// Load serialized coins or set defaults
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
            PlayerPrefs.SetInt(saveFbCoinsKey, 0); // reset facebook gift
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(CoinsHolder))]
    public class CoinsHolderEditor : Editor
    {
        private bool test = true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            CoinsHolder cH = (CoinsHolder)target;
            EditorGUILayout.LabelField("Count: " + CoinsHolder.Count);

            if (test = EditorGUILayout.Foldout(test, "Test"))
            {
                EditorGUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Add 500 coins"))
                {
                    CoinsHolder.Add(500);
                }
                if (GUILayout.Button("Set 500 coins"))
                {
                    cH.SetCount(500);
                }
                if (GUILayout.Button("Clear coins"))
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
                if (GUILayout.Button("Log coins"))
                {
                    Debug.Log("Coins: " + CoinsHolder.Count);

                }
                if (GUILayout.Button("Load saved coins"))
                {
                    cH.Load();
                }
            }
        }
    }
#endif
}