using UnityEngine;
using System;
using System.Collections.Generic;

namespace Mkey
{
    [CreateAssetMenu]
    public class GameObjectsSet : BaseScriptable
    {
        public Sprite[] backGrounds;

        public GameObject gridCellPrefab;

        public MahjongTile mahjongTilePrefab;

        #region get mahjong sprite


        /// <summary>
        /// first add simple sprites after that try adding groups - FillType.SimpleAndGroups
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private List<SpritesPair> GetRandomPairs_SAG(int count)  
        {
            List<SpritesPair> source = new List<SpritesPair>();
            List<SpritesPair> simple = new List<SpritesPair>();

            ThemeSpritesHolder themeSpritesHolder = GameThemesHolder.Instance.GetTheme();

            // first add simple sprites
            foreach (var item in themeSpritesHolder.simpleSprites)
            {
                if (simple.Count < count) simple.Add(new SpritesPair(item, item));
                else break;
            }
            source.AddRange(simple);

            // add complete groups
            foreach (var group_i in themeSpritesHolder.groups)
            {
                List<SpritesPair> groupSprites = group_i.GetSequencedSpritesPairs();
                if (source.Count + groupSprites.Count <= count)
                {
                    source.AddRange(groupSprites);
                }
                else break;
            }

            // add simple sprites
            int indexSimple = 0;
            while (source.Count < count)
            {
                source.Add(simple[indexSimple]);
                indexSimple++;
                if (indexSimple >= simple.Count) indexSimple = 0;
            }

            source.Shuffle();
            return source;
        }

        /// <summary>
        /// first add groups after that try adding simple sprites- FillType.GroupsAndSimple
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private List<SpritesPair> GetRandomPairs_GAS(int count)
        {
            List<SpritesPair> source = new List<SpritesPair>();
            ThemeSpritesHolder themeSpritesHolder = GameThemesHolder.Instance.GetTheme();

            // first add complete groups
            foreach (var group_i in themeSpritesHolder.groups)
            {
                List<SpritesPair> groupSprites = group_i.GetSequencedSpritesPairs();
                if (source.Count + groupSprites.Count <= count)
                {
                    source.AddRange(groupSprites);
                }
                else break;
            }

            // try to add simple sprites
            while (source.Count < count)
            {
                foreach (var item in themeSpritesHolder.simpleSprites)
                {
                    if (source.Count < count) source.Add(new SpritesPair(item, item));
                    else break;
                }
            }

            source.Shuffle();
            return source;
        }

        /// <summary>
        /// first add 1 random group after that try adding simple sprites- FillType.RandomGroupAndSimple
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private List<SpritesPair> GetRandomPairs_RGS(int count)
        {
            List<SpritesPair> source = new List<SpritesPair>();
            ThemeSpritesHolder themeSpritesHolder = GameThemesHolder.Instance.GetTheme();

            // first add complete groups
            if (themeSpritesHolder.groups.Count > 0)
            {
                List<SpritesPair> groupSprites = themeSpritesHolder.groups.GetRandomPos().GetSequencedSpritesPairs();
                if (source.Count + groupSprites.Count <= count)
                {
                    source.AddRange(groupSprites);
                }
            }

            // try to add simple sprites
            while (source.Count < count)
            {
                foreach (var item in themeSpritesHolder.simpleSprites)
                {
                    if (source.Count < count) source.Add(new SpritesPair(item, item));
                    else break;
                }
            }

            source.Shuffle();
            return source;
        }


        /// <summary>
        /// first add simple sprites after that try adding groups - FillType.Simple
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private List<SpritesPair> GetRandomPairs_S(int count)
        {
            List<SpritesPair> source = new List<SpritesPair>();
            ThemeSpritesHolder themeSpritesHolder = GameThemesHolder.Instance.GetTheme();

            // try to add simple sprites
            while (source.Count < count)
            {
                foreach (var item in themeSpritesHolder.simpleSprites)
                {
                    if (source.Count < count) source.Add(new SpritesPair(item, item));
                    else break;
                }
            }

            source.Shuffle();
            return source;
        }

        public List<SpritesPair> GetRandomPairs(int count, FillType fillType)
        {
            switch (fillType)
            {
                case FillType.OnlySimple:
                    return GetRandomPairs_S(count);
                case FillType.GroupsAndSimple:
                    return GetRandomPairs_GAS(count);
                case FillType.SimpleAndGroups:
                    return GetRandomPairs_SAG(count);
                case FillType.RandomGroupAndSimple:
                    return GetRandomPairs_RGS(count);
                default:
                    return GetRandomPairs_S(count);
            }
        }

        public bool IsOneGroup(Sprite sprite_1, Sprite sprite_2)
        {
            ThemeSpritesHolder themeSpritesHolder = GameThemesHolder.Instance.GetTheme(); ;
            return themeSpritesHolder.IsOneGroup(sprite_1, sprite_2);
        }
        #endregion get mahjong sprite

        #region background
        public Sprite GetBackGround(int index)
        {
            index = (int)Mathf.Repeat(index, backGrounds.Length);
            return backGrounds[index];
        }

        public int BackGroundsCount
        {
            get { return backGrounds.Length; }
        }
        #endregion background
    }

    [Serializable]
    public class CellData
    {
        [SerializeField]
        private int id;
        [SerializeField]
        private int row;
        [SerializeField]
        private int column;

        public int ID { get { return id; } }
        public int Row { get { return row; } }
        public int Column { get { return column; } }

        public CellData(int id, int row, int column)
        {
            this.row = row;
            this.column = column;
            this.id = id;
        }
    }

    /// <summary>
    /// Helper serializable class object with the equal ID
    /// </summary>
    [Serializable]
    public class ObjectsSetData
    {
        public Action <int> ChangeEvent;

        [SerializeField]
        private int id;
        [SerializeField]
        private int count;

        public int ID { get { return id; } }
        public int Count { get { return count; } }

        public ObjectsSetData(int id, int count)
        {
            this.id = id;
            this.count = count;
        }

        //public Sprite GetImage(GameObjectsSet mSet)
        //{
        //    return mSet.GetMainObject(id).ObjectImage;
        //}

        public void IncCount()
        {
            SetCount(count + 1);
        }

        public void DecCount()
        {
            SetCount(count - 1);
        }

        public void SetCount(int newCount)
        {
            newCount = Mathf.Max(0, newCount);
            bool changed = (Count != newCount);
            count = newCount;
            if(changed)  ChangeEvent?.Invoke(count);
        }
    }

    /// <summary>
    /// Helper class that contains list of object set data 
    /// </summary>
    [Serializable]
    public class ObjectSetCollection
    {
        [SerializeField]
        private List<ObjectsSetData> list;

        public IList<ObjectsSetData> ObjectsList { get { return list.AsReadOnly(); } }

        public ObjectSetCollection()
        {
            list = new List<ObjectsSetData>();
        }

        public ObjectSetCollection(ObjectSetCollection oSCollection)
        {
            list = new List<ObjectsSetData>();
            Add(oSCollection);
        }

        public ObjectSetCollection(List<ObjectsSetData> oSCollection)
        {
            list = new List<ObjectsSetData>();
            Add(oSCollection);
        }

        public uint Count
        {
            get { return (list == null) ? 0 : (uint)list.Count; }
        }

        public void AddById(int id, int count)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ID == id)
                {
                    list[i].SetCount(list[i].Count + count);
                    return;
                }
            }
            list.Add(new ObjectsSetData(id, count));
        }

        public void RemoveById(int id, int count)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ID == id)
                {
                    list[i].SetCount(list[i].Count - count);
                  //  if (list[i].Count == 0) list.RemoveAt(i);
                    return;
                }
            }
        }

        public void SetCountById(int id, int count)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ID == id)
                {
                    list[i].SetCount(count);
                    return;
                }
            }
            list.Add(new ObjectsSetData(id, count));
        }

        public void CleanById(int id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ID == id)
                {
                    list.RemoveAt(i);
                    return;
                }
            }
        }

        public int CountByID(int id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].ID == id)
                    return list[i].Count;
            }
            return 0;
        }

        public bool ContainObjectID(int id)
        {
            return CountByID(id) > 0;
        }

        public void Add(ObjectSetCollection oSCollection)
        {
            if (oSCollection != null)
            {
                foreach (var item in oSCollection.ObjectsList)
                {
                    AddById(item.ID, item.Count);
                }
            }
        }

        public void Add(List<ObjectsSetData> oSCollection)
        {
            if (oSCollection != null)
            {
                foreach (var item in oSCollection)
                {
                    AddById(item.ID, item.Count);
                }
            }
        }

        public void RemoveAll(Func<ObjectsSetData, bool> func)
        {
            list.RemoveAll((item) => { return func(item); });
        }
    }
}