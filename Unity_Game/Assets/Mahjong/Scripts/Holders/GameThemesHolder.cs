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
    public enum FillType {OnlySimple, GroupsAndSimple, SimpleAndGroups, RandomGroupAndSimple}

    [CreateAssetMenu(menuName = "ScriptableObjects/GameThemesHolder")]
    public class GameThemesHolder : SingletonScriptableObject<GameThemesHolder>
    {
        [SerializeField]
        public ThemeSpritesHolder [] themes;

        #region keys
        [SerializeField]
        private string saveKey = "mk_theme";
        #endregion keys

        #region temp vars
        private static bool loaded = false;
        #endregion temp vars

        private static int _themeIndex;

        public static int ThemeIndex
        {
            get { if (!loaded) Instance.Load(); return _themeIndex; }
            private set { _themeIndex = value; }
        }

        public UnityEvent<int> ChangeUnityEvent;
        public UnityEvent<int> LoadUnityEvent;
        public Action<int, int> ChangeEvent;    // old new
        public Action<int> LoadEvent;

        private void Awake()
        {
            Load();
            Debug.Log("Awake: " + this + " ; theme index: " + ThemeIndex);
        }

        /// <summary>
        /// Set new index
        /// </summary>
        /// <param name="count"></param>
        public void SetIndex(int index)
        {
            int oldIndex = ThemeIndex;
            // fName = string.IsNullOrEmpty(fName) ? FullName : fName;
            if (index < 0) index = 0;
            bool changed = (ThemeIndex != index);
            ThemeIndex = index;
            if (changed)
            {
                PlayerPrefs.SetInt(saveKey, ThemeIndex);
                ChangeEvent?.Invoke(oldIndex, ThemeIndex);
                ChangeUnityEvent?.Invoke(ThemeIndex);
            }
        }

        /// <summary>
        /// Load serialized data or set defaults
        /// </summary>
        public void Load()
        {
            loaded = true;
            _themeIndex = PlayerPrefs.GetInt(saveKey, 0);
            LoadEvent?.Invoke(ThemeIndex);
            LoadUnityEvent?.Invoke(ThemeIndex);
        }

        public void SetDefaultData()
        {
            PlayerPrefs.DeleteKey(saveKey);
            SetIndex(0);
        }

        public ThemeSpritesHolder GetTheme()
        {
            if (themes.Length == 0) return null;
            if (themes.Length > 0 && ThemeIndex >= 0 && ThemeIndex < themes.Length) return themes[ThemeIndex];
            return themes[themes.Length - 1];
        }

        public Dictionary <Sprite, Sprite> GetSpritesDictionary(ThemeSpritesHolder theme_1, ThemeSpritesHolder theme_2)
        {
            Dictionary<Sprite, Sprite> res = new Dictionary<Sprite, Sprite>();
            List<Sprite> sprites_1 = theme_1.GetSequencedSprites();
            List<Sprite> sprites_2 = theme_2.GetSequencedSprites();
            if (sprites_1.Count != sprites_2.Count) return null;

            for (int i = 0; i < sprites_1.Count; i++)
            {
                res.Add(sprites_1[i], sprites_2 [i]);
            }
            return res;
        }

        public ThemeSpritesHolder GetSpriteTheme(Sprite sprite)
        {
            foreach (var item in themes)
            {
                if (item.ContainSrite(sprite)) return item;
            }
            return null;
        }

        public List<Sprite> GetSpriteAliases(Sprite sourceSprite, bool includeSourceSprite)
        {
            if (sourceSprite == null) return null;
            List<Sprite> result = new List<Sprite>();
            ThemeSpritesHolder tH_0 = GetSpriteTheme(sourceSprite);
            int index = tH_0.GetSequencedSprites().IndexOf(sourceSprite);

            foreach (var item in themes)
            {
                if (item == tH_0 && !includeSourceSprite) continue;
                result.Add(item.GetSequencedSprites()[index]);
            }
            return result;
        }

        public Sprite GetSpriteAlias(Sprite sourceSprite)
        {
            if (sourceSprite == null) return null;
            ThemeSpritesHolder th_current = GetTheme();
            ThemeSpritesHolder th_s = GetSpriteTheme(sourceSprite);
            if (th_s == th_current) return sourceSprite;
            int index = th_s.GetSequencedSprites().IndexOf(sourceSprite);
            return th_current.GetSequencedSprites()[index];
        }
    }


/*
    [Serializable]
    public class MahjongSpriteID
    {
        public int indexInCollection;
        public int groupIndex;
        public int themeIndex;
    }
*/

#if UNITY_EDITOR
    [CustomEditor(typeof(GameThemesHolder))]
    public class GameThemesHolderEditor : Editor
    {
        private bool test = true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GameThemesHolder tH = (GameThemesHolder)target;
            EditorGUILayout.LabelField("Theme Index: " + GameThemesHolder.ThemeIndex);

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