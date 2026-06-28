using System.Collections.Generic;
using UnityEngine;

namespace Mkey
{
    public class Row<T> : CellArray<T> where T : GridCell
    {
        public Row(int size) : base(size) { }

        /// <summary>
        /// Get right cells
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public List<T> GetRightCells(int index)
        {
            List<T> cs = new List<T>();
            if (ok(index))
            {
                int i = Length - 1;
                while (i > index)
                {
                    cs.Add(cells[i]);
                    i--;
                }
            }
            return cs;
        }

        /// <summary>
        /// Get right cell
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T GetRightCell(int index)
        {
            if (ok(index + 1))
            {
                return cells[index + 1];
            }
            return null;
        }

        /// <summary>
        /// Get left cells
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public List<T> GetLeftCells(int index)
        {
            List<T> cs = new List<T>();
            if (ok(index))
            {
                int i = 0;
                while (i < index)
                {
                    cs.Add(cells[i]);
                    i++;
                }
            }
            return cs;
        }

        /// <summary>
        /// Get left cell
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T GetLeftCell(int index)
        {
            if (ok(index - 1))
            {
                return cells[index - 1];
            }
            return null;
        }
    }

    public class Column<T> : CellArray<T> where T : GridCell
    {
        public Column(int size) : base(size) { }

        /// <summary>
        /// Get cells at top
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public List<T> GetTopCells(int index)
        {
            List<T> cs = new List<T>();
            if (ok(index))
            {
                int i = 0;
                while (i < index)
                {
                    cs.Add(cells[i]);
                    i++;
                }
            }
            return cs;
        }

        /// <summary>
        /// Get cell at top
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T GetTopCell(int index)
        {
            if (ok(index - 1))
            {
                return cells[index - 1];
            }
            return null;
        }

        /// <summary>
        /// Get bottom cells
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public List<T> GetBottomCells(int index)
        {
            List<T> cs = new List<T>();
            if (ok(index))
            {
                int i = Length - 1;
                while (i > index)
                {
                    cs.Add(cells[i]);
                    i--;
                }
            }
            return cs;
        }

        /// <summary>
        /// Get bottom cell
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T GetBottomCell(int index)
        {
            if (ok(index + 1))
            {
                return cells[index - 1];
            }
            return null;
        }
    }

    public class CellArray<T> : GenInd<T> where T : GridCell
    {
        public CellArray(int size) : base(size) { }

        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < cells.Length; i++)
            {
                s += cells[i].ToString();
            }
            return s;
        }
    }

    public class GenInd<T> where T : class
    {
        public T[] cells;
        public int Length;

        public GenInd(int size)
        {
            cells = new T[size];
            Length = size;
        }

        public T this[int index]
        {
            get { if (ok(index)) { return cells[index]; } else { return null; } }
            set { if (ok(index)) { cells[index] = value; } else { } }
        }

        protected bool ok(int index)
        {
            return (index >= 0 && index < Length);
        }

        public T GetMiddleCell()
        {
            int number = Length / 2;

            return cells[number];
        }
    }

    public static class GCListExtension
    {
        public static List<GridCell> SortByDistanceTo(this List<GridCell> list, GridCell gC)
        {
            list.Sort(delegate (GridCell x, GridCell y) // x==y ->0; x>y ->1; x<y -1
            {
                if (x == null)
                {
                    if (y == null)
                    {
                        return 0;// If x is null and y is null, they're equal. 
                    }
                    else
                    {
                        return -1;// If x is null and y is not null, yis greater. 
                    }
                }
                else
                {
                    float xDist = Vector2.Distance(x.transform.position, gC.transform.position);
                    float yDist = Vector2.Distance(y.transform.position, gC.transform.position);
                    if (xDist == yDist) return 0;
                    if (xDist > yDist) return -1;
                    return 1;
                }
            });
            return list;
        }
    }

    public class MatchPair
    {
        internal MahjongTile mahjongTile_1;
        internal MahjongTile mahjongTile_2;
        public bool IsGoodPaar { get; private set; }

        public MatchPair(MahjongTile mahjongTile_1, MahjongTile mahjongTile_2)
        {
            this.mahjongTile_1 = mahjongTile_1;
            this.mahjongTile_2 = mahjongTile_2;
            if (mahjongTile_1 == mahjongTile_2) IsGoodPaar = false;
        }

        public bool IsEqualTo(MatchPair other)
        {
            if (!IsGoodPaar || !other.IsGoodPaar) return false;
            bool firstEqual = (this.mahjongTile_1 == other.mahjongTile_1) || (this.mahjongTile_1 == other.mahjongTile_2);
            bool secondEqual = (this.mahjongTile_2 == other.mahjongTile_2) || (this.mahjongTile_2 == other.mahjongTile_1);

            return firstEqual && secondEqual;
        }

        public bool IsComplete()
        {
            return mahjongTile_1 != null && mahjongTile_2 != null;
        }

        public bool ContaiAny(MahjongTile mahjongTile_1, MahjongTile mahjongTile_2)
        {
            bool firstEqual = (this.mahjongTile_1 == mahjongTile_1) || (this.mahjongTile_1 == mahjongTile_2);
            bool secondEqual = (this.mahjongTile_2 == mahjongTile_2) || (this.mahjongTile_2 == mahjongTile_1);

            return firstEqual || secondEqual;
        }
    }

    public class SpritesPair
    {
        internal Sprite sprite_1;
        internal Sprite sprite_2;

        public SpritesPair(Sprite sprite_1, Sprite sprite_2)
        {
            this.sprite_1 = sprite_1;
            this.sprite_2 = sprite_2;
        }
    }

    public class PossibleMatches
    {
        public List<MatchPair> matchPairs;
        public int Count => matchPairs.Count;
        public PossibleMatches(List<MahjongTile> freeToMatchTiles)
        {
            matchPairs = new List<MatchPair>();
            if (freeToMatchTiles == null || freeToMatchTiles.Count == 0) return;

            while (freeToMatchTiles.Count > 0)
            {
                var mTile = freeToMatchTiles[0];
                freeToMatchTiles.RemoveAt(0);

                foreach (var item in freeToMatchTiles)
                {
                    if (mTile.SpriteCanMatchhWith(item.MSprite))
                    {
                        MatchPair freePair = new MatchPair(mTile, item);
                        AddMatchPair(freePair);
                    }
                }
            }
        }

        private void AddMatchPair(MatchPair newPair)
        {
            if (!ContainMatchPair(newPair)) matchPairs.Add(newPair);
        }

        public bool ContainMatchPair(MatchPair freePaar)
        {
            foreach (var item in matchPairs)
            {
                if (item.IsEqualTo(freePaar)) return true;
            }
            return false;
        }

        public MatchPair GetMatchPair(int number)
        {
            return matchPairs[number];
        }
    }
}
