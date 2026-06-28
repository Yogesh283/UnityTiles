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
    [CreateAssetMenu(menuName = "ScriptableObjects/AvatarsHolder")]
    public class AvatarsHolder : SingletonScriptableObject<AvatarsHolder>
    {
        [SerializeField]
        public Sprite [] avatars;

        #region keys
        [SerializeField]
        private string saveKey = "mk_avatar";
        #endregion keys

        #region temp vars
        private static bool loaded = false;
        #endregion temp vars

        private static int _avatarIndex;

        public static int AvatarIndex
        {
            get { if (!loaded) Instance.Load(); return _avatarIndex; }
            private set { _avatarIndex = value; }
        }

        public UnityEvent<int> ChangeUnityEvent;
        public UnityEvent<int> LoadUnityEvent;
        public Action<int> ChangeEvent;
        public Action<int> LoadEvent;

        private void Awake()
        {
            Load();
            Debug.Log("Awake: " + this + " ; avatar index: " + AvatarIndex);
        }

        /// <summary>
        /// Set new index
        /// </summary>
        /// <param name="count"></param>
        public void SetIndex(int index)
        {
            // fName = string.IsNullOrEmpty(fName) ? FullName : fName;
            if (index < 0) index = 0;
            bool changed = (AvatarIndex != index);
            AvatarIndex = index;
            if (changed)
            {
                PlayerPrefs.SetInt(saveKey, AvatarIndex);
                ChangeEvent?.Invoke(AvatarIndex);
                ChangeUnityEvent?.Invoke(AvatarIndex);
            }
        }

        /// <summary>
        /// Load serialized data or set defaults
        /// </summary>
        public void Load()
        {
            loaded = true;
            _avatarIndex = PlayerPrefs.GetInt(saveKey, 0);
            LoadEvent?.Invoke(AvatarIndex);
            LoadUnityEvent?.Invoke(AvatarIndex);
        }

        public void SetDefaultData()
        {
            PlayerPrefs.DeleteKey(saveKey);
            SetIndex(0);
        }

        public Sprite GetAvatarSprite()
        {
            if (avatars.Length == 0) return null;
            if (avatars.Length > 0 && AvatarIndex >= 0 && AvatarIndex < avatars.Length) return avatars[AvatarIndex];
            return avatars[avatars.Length - 1];
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AvatarsHolder))]
    public class AvatarsHolderEditor : Editor
    {
        private bool test = true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            AvatarsHolder tH = (AvatarsHolder)target;
            EditorGUILayout.LabelField("Avatar Index: " + AvatarsHolder.AvatarIndex);

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