using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Mkey
{
    public class GameConstructSet : BaseScriptable
    {
        [SerializeField]
        private GameObjectsSet gOSet;
        [Space(8, order = 0)]
        [Header("Constructed Levels", order = 1)]
        public List<LevelConstructSet> levelSets;

        public bool testMode = false;
        public int testLevel = 0;

        public static int MaxLayersCount = 5;

        #region properties
        public GameObjectsSet GOSet { get { return gOSet; } }

        public int LevelCount
        {
            get { if (levelSets != null) return levelSets.Count; else return 0; }
        }
        #endregion properties

        static GameConstructSet _instance = null;
        public static GameConstructSet Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = Resources.FindObjectsOfTypeAll<GameConstructSet>().FirstOrDefault();
                }

#if UNITY_EDITOR
                if (!_instance)
                {
                    string[] guids2 = UnityEditor.AssetDatabase.FindAssets("GameConstructSet", null);
                    foreach (var item in guids2)
                    {
                        Debug.Log(item);
                    }
                    if (guids2 != null && guids2.Length > 0)
                    {
                        _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConstructSet>(guids2[0]); // UnityEditor.AssetDatabase.LoadAssetAtPath<GameConstructSet>("Assets/Resources/GameConstaructSet/GameConstructSet_1.asset");
                    }
                }
#endif
                return _instance;
            }
        }

        /// <summary>
        /// Return LevelConstructSet for levelNumber. If levelNumber out of range - return LevelConstruct for 1 levelNumber.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public LevelConstructSet GetLevelConstructSet(int level)
        {
            if (InRange(level)) return levelSets[level];
            else if (levelSets != null) return levelSets[levelSets.Count - 1];
            return null;
        }

        #region regular
        private void OnEnable()
        {

        }

        #endregion regular

        public void Clean()
        {
            bool needClean = false;
            if (levelSets == null) { levelSets = new List<LevelConstructSet>(); needClean = true; };

            if (!needClean)
                foreach (var item in levelSets)
                {
                    if (item == null)
                    {
                        needClean = true;
                        break;
                    }
                }

            if (needClean)
            {
                levelSets = levelSets.Where(item => item != null).ToList();
                SetAsDirty();
            }
            Debug.Log("levels count " + levelSets.Count);
        }

        public void AddLevel(LevelConstructSet lc)
        {
            if (levelSets == null) { levelSets = new List<LevelConstructSet>(); }
            levelSets.Add(lc);
            SetAsDirty();
        }

        public void InsertBeforeLevel(int levelIndex, LevelConstructSet lcs)
        {
            if (!InRange(levelIndex)) return;
            levelSets.Insert(levelIndex, lcs);
            SetAsDirty();
        }

        public void InsertAfterLevel(int levelIndex, LevelConstructSet lcs)
        {
            Debug.Log("insert level after: " + levelIndex);
            if (!InRange(levelIndex)) return;
            if (levelIndex + 1 == levelSets.Count)
            {
                levelSets.Add(lcs);
                Debug.Log("add after : " + levelIndex);
            }
            else
            {
                levelSets.Insert(levelIndex + 1, lcs);
                Debug.Log("insert after : " + levelIndex);
            }
            SetAsDirty();
        }

        public void RemoveLevel(int levelIndex)
        {
            if (!InRange(levelIndex)) return;
#if UNITY_EDITOR
                ScriptableObjectUtility.DeleteResourceAsset(levelSets[levelIndex]);
#endif
            levelSets.RemoveAt(levelIndex);
            SetAsDirty();
        }

        private bool InRange(int level)
        {
            return (levelSets != null && levelSets.Count > level && level >= 0);
        }
    }
}

