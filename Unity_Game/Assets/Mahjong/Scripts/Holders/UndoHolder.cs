using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace Mkey
{
    [CreateAssetMenu(menuName = "ScriptableObjects/UndoHolder")]
    public class UndoHolder : SingletonScriptableObject<UndoHolder>
    {
        [Space(10, order = 0)]
        [Header("Default data", order = 1)]
        [Tooltip("Default count at start")]
        [SerializeField]
        private int defCount = 20;

        [SerializeField]
        private string saveKey = "mk_mahjong_undo";

        private static bool loaded = false;
        private static int _count;

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
        }

        public static void Add(int count)
        {
            if (Instance)
                Instance.SetCount(Count + count);
        }

        public void SetCount(int count)
        {
            count = Mathf.Max(0, count);
            bool changed = Count != count;
            Count = count;
            if (changed)
                PlayerPrefs.SetInt(saveKey, Count);
            if (changed)
                ChangeEvent?.Invoke(Count);
        }

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
    [CustomEditor(typeof(UndoHolder))]
    public class UndoHolderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            UndoHolder holder = (UndoHolder)target;
            EditorGUILayout.LabelField("Count: " + UndoHolder.Count);
        }
    }
#endif
}
