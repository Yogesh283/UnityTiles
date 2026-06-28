using UnityEngine;
using UnityEngine.Events;
using System;
using System.Globalization;
using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
#endif

/*
  player data holder 
  26.06.2021
 */
namespace Mkey
{
    [CreateAssetMenu(menuName = "ScriptableObjects/PlayerDataHolder")]
    public class PlayerDataHolder : SingletonScriptableObject<PlayerDataHolder>
    {
        #region default data
        [Space(10, order = 0)]
        [Header("Default data", order = 1)]

        [Tooltip("Default player name")]
        [SerializeField]
        private string defFullName = "Good Player";
        #endregion default data

        #region keys
        [SerializeField]
        private string saveKey = "mk_fullname";
        #endregion keys

        #region temp vars
        private static bool loaded = false;
        #endregion temp vars
        private static string _fullName;

        public static string FullName
        {
            get { if (!loaded) Instance.Load(); return _fullName; }
            private set { _fullName = value; }
        }

        public UnityEvent<string> ChangeUnityEvent;
        public UnityEvent<string> LoadUnityEvent;
        public Action<string> ChangeEvent;
        public Action<string> LoadEvent;

        private void Awake()
        {
            Load();
            Debug.Log("Awake: " + this + " ; full name: " + FullName);
        }

        /// <summary>
        /// Set full name
        /// </summary>
        /// <param name="count"></param>
        public void SetFullName(string fName)
        {
            fName = string.IsNullOrEmpty(fName) ? FullName : fName;
            bool changed = (FullName != fName);
            FullName = fName;
            if (changed)
            {
                PlayerPrefs.SetString(saveKey, FullName);
                ChangeEvent?.Invoke(FullName);
                ChangeUnityEvent?.Invoke(FullName);
            }
        }

        /// <summary>
        /// Load serialized data or set defaults
        /// </summary>
        public void Load()
        {
            loaded = true;
            _fullName = PlayerPrefs.GetString(saveKey, defFullName);
            LoadEvent?.Invoke(FullName);
            LoadUnityEvent?.Invoke(FullName);
        }

        public void SetDefaultData()
        {
            PlayerPrefs.DeleteKey(saveKey);
            SetFullName(defFullName);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PlayerDataHolder))]
    public class PlayerDataHolderEditor : Editor
    {
        private bool test = true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            PlayerDataHolder tH = (PlayerDataHolder)target;
            EditorGUILayout.LabelField("Full Name: " + PlayerDataHolder.FullName);

            #region test
            if (test = EditorGUILayout.Foldout(test, "Test"))
            {
                EditorGUILayout.BeginHorizontal("box");

                if (GUILayout.Button("Reset to default"))
                {
                    tH.SetDefaultData();
                }

                if (GUILayout.Button("Log data"))
                {
                    Debug.Log("Player data: " );

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