using UnityEngine;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
#endif

namespace Mkey
{
    [CreateAssetMenu(menuName = "ScriptableObjects/ThemeSpritesHolder")]
    public class ThemeSpritesHolder : BaseScriptable
    {
        public List<Sprite> simpleSprites;
        public List<MahjongSpritesGroup> groups;
        public string themeName;

        public int GetSpritesCount()
        {
            int count = 0;
            if (groups != null)
            {
                foreach (var item in groups)
                {
                    if (item != null) count += item.Count;
                }
            }
            if (simpleSprites != null) count = simpleSprites.Count + count;
            return count;
        }

        public string FailedSpritesIndexes()
        {
            string res = "";
            if (simpleSprites != null)
            {
                for (int i = 0; i < simpleSprites.Count; i++)
                {
                    if (simpleSprites[i] == null) res += "failed simple collection index: " + i + '\n';
                }
            }

            if (groups != null)
            {
                for (int gri = 0; gri < groups.Count; gri++)
                {
                    MahjongSpritesGroup mSGR = groups[gri];
                    if (mSGR != null)
                    {
                        for (int i = 0; i < mSGR.Count; i++)
                        {
                            if (mSGR.collection[i] == null) res += "group " + gri + "; failed collection index: " + i + '\n';
                        }
                    }
                }
            }
            return res;
        }

        public bool IsOneGroup(Sprite sprite_1, Sprite sprite_2)
        {
            foreach (var group_i in groups)
            {
                if (group_i.Contains(sprite_1) && group_i.Contains(sprite_2)) return true;
            }
            return false;
        }

        public List<Sprite> GetSequencedSprites()
        {
            List<Sprite> res = new List<Sprite>(simpleSprites);
            foreach (var item in groups)
            {
                res.AddRange(item.collection);
            }
            return res;
        }

        public bool ContainSrite(Sprite sprite)
        {
            if (simpleSprites.Contains(sprite)) return true;
            for (int gri = 0; gri < groups.Count; gri++)
            {
                MahjongSpritesGroup mSGR = groups[gri];
                if (mSGR.Contains(sprite)) return true;
            }
            return false;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ThemeSpritesHolder))]
    public class ThemeSpritesHolderEditor : Editor
    {
        private bool test = true;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            ThemeSpritesHolder tH = (ThemeSpritesHolder)target;
            EditorGUILayout.LabelField("Sprites Count: " + tH.GetSpritesCount());

            #region test
            if (test = EditorGUILayout.Foldout(test, "Test"))
            {
                EditorGUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Log data"))
                {
                    Debug.Log("Theme name - " + tH.themeName );
                    string failedData = tH.FailedSpritesIndexes();
                    if(!string.IsNullOrEmpty(failedData)) Debug.Log(failedData);
                    else
                    {
                        Debug.Log(tH.themeName + " - No failed sprites in collection found");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            #endregion test
        }
    }
#endif

    [Serializable]
    public class MahjongSpritesGroup
    {
        public List<Sprite> collection;
        public int Count => (collection == null) ? 0 : collection.Count;

        public bool Contains(Sprite sprite)
        {
            return (collection != null && collection.Contains(sprite));
        }

        public virtual List<SpritesPair> GetSequencedSpritesPairs()
        {
            int count = (collection.Count % 2 == 0) ? collection.Count : collection.Count - 1;
            List<SpritesPair> res = new List<SpritesPair>();
            for (int i = 0; i < count; i+=2)
            {
                res.Add(new SpritesPair(collection[i], collection[i + 1]));
            }
            return res;
        }
    }

    [Serializable]
    public class MahjongSpritesGroupSimple : MahjongSpritesGroup
    {
        public override List<SpritesPair> GetSequencedSpritesPairs()
        {
            int count = (collection.Count % 2 == 0) ? collection.Count : collection.Count - 1;
            List<SpritesPair> res = new List<SpritesPair>();
            for (int i = 0; i < count; i ++)
            {
                res.Add(new SpritesPair(collection[i], collection[i]));
            }
            return res;
        }
    }
 }