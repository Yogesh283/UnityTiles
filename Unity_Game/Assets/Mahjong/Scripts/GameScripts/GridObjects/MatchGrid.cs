using System.Collections.Generic;
using UnityEngine;

namespace Mkey
{
    public class MatchGrid
    {
        private GameObjectsSet goSet;
        public List<Column<GridCell>> Columns { get; private set; }
        public List<GridCell> Cells { get; private set; }
        public List<Row<GridCell>> Rows { get; private set; }
        public Transform Parent { get; private set; }
        private GameMode gMode;
        private int vertSize;
        private int horSize;
        private GameObject prefab;
        private float yStart;
        private float yStep;
        private float xStep;
        private int yOffset;
        private Vector2 cellSize;
        private float cOffset;

        public LevelConstructSet LcSet { get; private set; }
        private List<MahjongTile> tiles;

        #region ctor, create
        public MatchGrid(LevelConstructSet lcSet, GameObjectsSet goSet, Transform parent, GameMode gMode)
        {
            this.LcSet = lcSet;
            this.Parent = parent;
            this.gMode = gMode;
            Debug.Log("new grid " + lcSet.name);

            vertSize = lcSet.VertSize;
            horSize = lcSet.HorSize;
            this.goSet = goSet;
            prefab = goSet.gridCellPrefab;
            cellSize = prefab.GetComponent<BoxCollider2D>().size;

            float deltaX = lcSet.DistX;
            float deltaY = lcSet.DistY;
            SetScale(lcSet.Scale);

            Cells = new List<GridCell>(vertSize * horSize);
            Rows = new List<Row<GridCell>>(vertSize);

            yOffset = 0;
            xStep = (cellSize.x + deltaX);
            yStep = (cellSize.y + deltaY);

            cOffset = (1 - horSize) * xStep / 2.0f; // offset from center by x-axe
            yStart = (vertSize - 1) * yStep / 2.0f;

            //instantiate cells
            for (int i = 0; i < vertSize; i++)
            {
                AddRow();
            }
            InitCells();
            Debug.Log("Created Grid Cells: " + Cells.Count);
            SetObjectsData(lcSet, gMode, -1);   // -1 set all tiles
        }

        public void Rebuild(GameObjectsSet mSet, GameMode gMode)
        {
            Debug.Log("rebuild ");

            this.LcSet = LcSet;
            vertSize = LcSet.VertSize;
            horSize = LcSet.HorSize;

            float deltaX = LcSet.DistX;
            float deltaY = LcSet.DistY;
            SetScale(LcSet.Scale);

            this.goSet = mSet;
            Cells.ForEach((c) => { c.DestroyGridObjects(); });

            List<GridCell> tempCells = Cells;
            Cells = new List<GridCell>(vertSize * horSize + horSize);
            Rows = new List<Row<GridCell>>(vertSize);

            xStep = (cellSize.x + deltaX);
            yStep = (cellSize.y + deltaY);

            cOffset = (1 - horSize) * xStep / 2.0f; // offset from center by x-axe
            yStart = (vertSize - 1) * yStep / 2.0f;

            // create rows 
            GridCell cell;
            Row<GridCell> row;
            int cellCounter = 0;
            int ri = 0;

            for (int i = 0; i < vertSize; i++)
            {
                row = new Row<GridCell>(horSize);

                for (int j = 0; j < row.Length; j++)
                {
                    Vector3 pos = new (j * xStep + cOffset, yStart - i * yStep, 0);

                    if (tempCells != null && cellCounter < tempCells.Count)
                    {
                        cell = tempCells[cellCounter];
                        cell.gameObject.SetActive(true);
                        cell.transform.localPosition = pos;
                        cellCounter++;
                        SpriteRenderer sR = cell.GetComponent<SpriteRenderer>();
                        if (sR)
                        {
                            sR.enabled = true;
                        }
                    }
                    else
                    {
                        cell = UnityEngine.Object.Instantiate(mSet.gridCellPrefab).GetComponent<GridCell>();
                        cell.transform.parent = Parent;
                        cell.transform.localPosition = pos;
                        cell.transform.localScale = Vector3.one;
                    }


                    Cells.Add(cell);
                    row[j] = cell;
                }
                Rows.Add(row);
                ri++;
            }

            // destroy not used cells
            if (cellCounter < tempCells.Count)
            {
                for (int i = cellCounter; i < tempCells.Count; i++)
                {
                    UnityEngine.Object.Destroy(tempCells[i].gameObject);
                }
            }

            // cache columns
            Columns = new List<Column<GridCell>>(horSize);
            Column<GridCell> column;
            for (int c = 0; c < horSize; c++)
            {
                column = new Column<GridCell>(Rows.Count);
                for (int r = 0; r < Rows.Count; r++)
                {
                    column[r] = Rows[r][c];
                }
                Columns.Add(column);
            }

            Debug.Log("objects count: " + Parent.GetComponentsInChildren<MahjongTile>(true).Length);
            InitCells();
            SetObjectsData(LcSet, gMode, -1);   // -1 set all tiles

            Debug.Log("rebuild cells: " + Cells.Count);
        }


        /// <summary>
        /// set objects data from featured list to grid; 
        /// countLimit == -1 -> without limit, set all tiles
        /// </summary>
        /// <param name="lcSet"></param>
        /// <param name="gMode"></param>
        /// <param name="countLimit"> </param>
        internal void SetObjectsData(LevelConstructSet lcSet, GameMode gMode, int countLimit)
        {
            tiles = new List<MahjongTile>();
            if (countLimit < 0) countLimit = int.MaxValue;
            if (lcSet.cells != null)
            {
                bool canSetNextLayer = true;
                for (int layer = 0; layer < GameConstructSet.MaxLayersCount; layer++)
                {
                    if (canSetNextLayer)
                    {
                        canSetNextLayer = false;
                        // Debug.Log("Fill layer #" + layer);
                        int objectsCount = 0;
                        foreach (var c in lcSet.cells)
                        {
                            if (c != null && c.gridObjects != null)
                            {
                                foreach (var o in c.gridObjects)
                                {
                                    if (c.row >= 0 && c.column >= 0 && c.row < Rows.Count && c.column < Rows[c.row].Length && o.layer == layer && tiles.Count < countLimit)
                                    {
                                        GridObject gO = Rows[c.row][c.column].SetObject(goSet.mahjongTilePrefab, layer);
                                        MahjongTile _tile = gO ? (MahjongTile)gO : null;

                                        if (_tile)
                                        {
                                            tiles.Add(_tile);
                                            objectsCount++;
                                            canSetNextLayer = true;
                                        }
                                    }
                                }
                            }
                        }
                        Debug.Log("Layer #" + layer + "; objects count: " + objectsCount);
                    }
                }

                // remove the last odd tile
                if (gMode == GameMode.Play && tiles.Count % 2 != 0)
                {
                    MahjongTile mahjongTile = tiles[tiles.Count - 1];
                    Debug.Log("remove object: " + mahjongTile.ParentCell + "; layer: " + mahjongTile.Layer);
                    mahjongTile.ParentCell.RemoveObject(mahjongTile.Layer, false);
                    tiles.RemoveAt(tiles.Count - 1);
                }

                // cache raw blockers
                if (gMode == GameMode.Play)
                {
                    foreach (var item in tiles)
                    {
                        item.CacheRawBlockers();
                    }
                }
            }
        }

        public void CacheBlockers()
        {
            MahjongTile[] mahjongTiles = Parent.GetComponentsInChildren<MahjongTile>(true);
            foreach (var item in mahjongTiles)
            {
                item.CacheRawBlockers();
            }
        }

        /// <summary>
        /// Add row to grid
        /// </summary>
        private void AddRow()
        {
            GridCell cell;
            Row<GridCell> row = new Row<GridCell>(horSize);
            for (int j = 0; j < row.Length; j++)
            {
                Vector3 pos = new Vector3(j * xStep + cOffset, yStart + yOffset * yStep, 0);
                cell = UnityEngine.Object.Instantiate(goSet.gridCellPrefab).GetComponent<GridCell>();
                cell.transform.parent = Parent;
                cell.transform.localPosition = pos;
                cell.transform.localScale = Vector3.one;
                Cells.Add(cell);
                row[j] = cell;
            }

            Rows.Add(row);

            // cache columns
            Columns = new List<Column<GridCell>>(horSize);
            Column<GridCell> column;
            for (int c = 0; c < horSize; c++)
            {
                column = new Column<GridCell>(Rows.Count);
                for (int r = 0; r < Rows.Count; r++)
                {
                    column[r] = Rows[r][c];
                }
                Columns.Add(column);
            }
            yOffset--;
        }

        public GridCell this[int index0, int index1]
        {
            get {if (ok(index0, index1)) { return Rows[index0][index1]; } else { return null; } }
            set { if (ok(index0, index1)) { Rows[index0][index1] = value; } else { } }
        }

        private bool ok(int index0, int index1)
        {
            return (index0 >= 0 && index0 < vertSize && index1 >= 0 && index1 < horSize);
        }

        private void InitCells()
        {
            // init layer 0
            int layer = 0;
            for (int r = 0; r < Rows.Count; r++)
            {
                for (int c = 0; c < horSize; c++)
                {
                    Rows[r][c].Init(r, c, layer, Columns[c], Rows[r], this, gMode);
                }
            }
        }

        public void SetTofrontAll(bool setTofront)
        {
            MahjongTile[] mahjongTiles = Parent.GetComponentsInChildren<MahjongTile>(true);
            for (int i = 0; i < mahjongTiles.Length; i++)
            {
                mahjongTiles[i].SetToFront(setTofront);
            }
        }
        #endregion ctor, create

        #region  get data from grid
        public void CalcObjects()
        {
            GridObject[] bds = Parent.GetComponentsInChildren<GridObject>(true);
            Debug.Log("Objects count: " + bds.Length);
        }

        public Row<GridCell> GetRow(int row)
        {
            return (row >= 0 && row < Rows.Count) ? Rows[row] : null;
        }

        public Column<GridCell> GetColumn(int col)
        {
            return (col >= 0 && col < Columns.Count) ? Columns[col] : null;
        }

        public List<MahjongTile> GetFreeToMatchTiles()
        {
            List<MahjongTile> result = new List<MahjongTile>(Parent.GetComponentsInChildren<MahjongTile>(true));
            result.RemoveAll((t) => { return !t.IsFreeToMatch(); });
            return result;
        }

        public List<MahjongTile> GetLayerObjects(int layer)
        {
            List<MahjongTile> result = new List<MahjongTile>(Parent.GetComponentsInChildren<MahjongTile>(true));
            result.RemoveAll((t) => { return t.Layer != layer; });
            return result;
        }

        public int GetMaxLayer()
        {
            int maxLayer = 0;
            List<MahjongTile> result = new List<MahjongTile>(Parent.GetComponentsInChildren<MahjongTile>(true));

            foreach (var item in result)
            {
                if (item.Layer > maxLayer) maxLayer = item.Layer;
            }
            return maxLayer;

        }

        public MahjongTile [] GetTiles()
        {
            return Parent.GetComponentsInChildren<MahjongTile>(true);
        }
        #endregion  get data from grid

        #region fill sprites
        public void SetMahjongSprites()
        {
            // set majong sprites
            List<SpritesPair> sprites = goSet.GetRandomPairs(tiles.Count / 2, LcSet.fillType);
            List<MahjongTile> tT = new List<MahjongTile>(tiles);
            Debug.Log("set sprites, tiles count: " + tiles.Count + "; sprites pairs count: " + sprites.Count + "; " + LcSet.fillType);

            // 1 type - get random from free to fill tiles
            bool failed = false;
            for (int i = 0; i < tT.Count; i += 2)
            {
                List<MahjongTile> freeTiles = GetFreeToFillTiles(tiles, true, false);      // not sorted by layer
                if(freeTiles.Count < 5) freeTiles = GetFreeToFillTiles(tiles, true, true); // avoid last error (tile 0 over tile)
                if(freeTiles.Count == 1)
                {
                    failed = true;
                    break;
                }
                MahjongTile t1 = freeTiles[0];
                MahjongTile t2 = freeTiles[1];
                freeTiles[0].SetExcluded(true);
                freeTiles[1].SetExcluded(true);
                SpritesPair s = sprites[i / 2];
                t1.SetSprite(s.sprite_1);
                t2.SetSprite(s.sprite_2);
            }
            if (!failed) { Debug.Log("1 sampling type used."); return; }

            // 2 type -  get first from most top layer, second from most bottom layer
            if (failed)
            {
                tiles.ForEach((t) => { t.SetExcluded(false); });
                for (int i = 0; i < tT.Count; i += 2)
                {
                    List<MahjongTile>  freeTiles = GetFreeToFillTiles(tiles, true, true); // reverse sorted
                    if (freeTiles.Count == 1)
                    {
                        failed = true;
                        break;
                    }
                    MahjongTile t1 = freeTiles[0];
                    MahjongTile t2 = freeTiles[freeTiles.Count - 1];
                    freeTiles[0].SetExcluded(true);
                    freeTiles[1].SetExcluded(true);
                    SpritesPair s = sprites[i/2];
                    t1.SetSprite(s.sprite_1);
                    t2.SetSprite(s.sprite_2);
                }
            }
            if (!failed) { Debug.Log("2 sampling type used."); return; }

            // 3 type - get 2 tiles from most top layers
            if (failed)
            {
                tiles.ForEach((t)=> { t.SetExcluded(false); });
                failed = false;
                for (int i = 0; i < tT.Count; i += 2)
                {
                    List<MahjongTile> freeTiles = GetFreeToFillTiles(tiles, true, true); // reverse sorted
                    if (freeTiles.Count == 1)
                    {
                        failed = true;
                        break;
                    }
                    MahjongTile t1 = freeTiles[0];
                    MahjongTile t2 = freeTiles[1];
                    freeTiles[0].SetExcluded(true);
                    freeTiles[1].SetExcluded(true);
                    SpritesPair s = sprites[i / 2];
                    t1.SetSprite(s.sprite_1);
                    t2.SetSprite(s.sprite_2);
                }
            }
            if (!failed) Debug.Log("3 sampling type used.");
            else Debug.LogError("Fill failed, make changes in game board.");
        }

        private List<MahjongTile> GetFreeToFillTiles(List<MahjongTile>fromTiles, bool shuffle, bool sortByLayerReverse)
        {
            List<MahjongTile> result = new List<MahjongTile>(fromTiles);
            result.RemoveAll((t) => { return t == null; });
            result.RemoveAll((t) => { return t.Excluded; });
            result.RemoveAll((t) => { return !t.IsFreeToFill(); });
            if (shuffle) result.Shuffle();
            if (sortByLayerReverse) result.Sort((a,b) =>{return b.Layer.CompareTo(a.Layer);});
            return result;
        }
        #endregion fill sprites

        public void DebugDrawGrid()
        {
            Color color = Color.red;
            foreach (var item in Rows)
            {
                Vector3 pos_1 = item[0].transform.position;
                Vector3 pos_2 = item[item.Length-1].transform.position;
                Debug.DrawLine(pos_1, pos_2, color);
            }
        }

        /// <summary>
        /// shuffle tiles in current positions
        /// </summary>
        public void ShuffleGridSprites()
        {
            List<GridCell> gridCells = new List<GridCell>();
            List<MahjongTile> mahjongTiles = new List<MahjongTile>(Parent.GetComponentsInChildren<MahjongTile>(true));
            mahjongTiles.ForEach((t)=> { t.SetExcluded(false);});
            List<SpritesPair> spritesPairs = new List<SpritesPair>();

            while (mahjongTiles.Count > 0)   // get the list of match pairs
            {
                var mTile = mahjongTiles[0];
                mahjongTiles.RemoveAt(0);
                MahjongTile pairTile = null;
                foreach (var item in mahjongTiles)
                {
                    if (mTile.SpriteCanMatchhWith(item.MSprite))
                    {
                        pairTile = item;
                        SpritesPair freePaar = new SpritesPair(mTile.MSprite, item.MSprite);
                        spritesPairs.Add(freePaar);
                        break;
                    }
                }
                mahjongTiles.Remove(pairTile);
            }

            mahjongTiles = new List<MahjongTile>(Parent.GetComponentsInChildren<MahjongTile>(true));
            bool failed = false;
            foreach (var item in spritesPairs)
            {
                List<MahjongTile> freeTiles = GetFreeToFillTiles(mahjongTiles, true, true); // reverse sorted
                if (freeTiles.Count == 1)
                {
                    failed = true;
                    break;
                }
                MahjongTile t1 = freeTiles[0];
                MahjongTile t2 = freeTiles[1];
                freeTiles[0].SetExcluded(true);
                freeTiles[1].SetExcluded(true);
                t1.SetSprite(item.sprite_1);
                t2.SetSprite(item.sprite_2);
            }
            if(failed) Debug.LogError("!!! MIX FAILED !!!");
        }

        public void ReplaceMahjongSprites(ThemeSpritesHolder oldTheme, ThemeSpritesHolder newTheme)
        {
            Dictionary<Sprite, Sprite> res = GameThemesHolder.Instance.GetSpritesDictionary(oldTheme, newTheme);
            MahjongTile [] tT = Parent.GetComponentsInChildren<MahjongTile>();
            foreach (var item in tT)
            {
                item.SetSprite(res[item.MSprite]);
            }
        }

        public void SetScale(float scale)
        {
            Parent.localScale = new Vector3(scale, scale, scale);
        }


        /// <summary>
        /// Is it possible to shuffle tiles in current positions?
        /// </summary>
        /// <returns></returns>
        public bool CanShuffle()
        {
            List<MahjongTile> mahjongTiles = new List<MahjongTile>(Parent.GetComponentsInChildren<MahjongTile>(true));
            mahjongTiles.ForEach((t) => { t.SetExcluded(false); });
            List<MahjongTile> freeTiles;
            for (int i = 0; i < mahjongTiles.Count; i += 2)
            {
                freeTiles = GetFreeToFillTiles(mahjongTiles, false, false);
                if (freeTiles.Count == 1)
                {
                    return false;
                }
                freeTiles[0].SetExcluded(true);
                freeTiles[1].SetExcluded(true);
            }
            return true;
        }

        /// <summary>
        /// shuffle tiles into new positions
        /// </summary>
        public void HardShuffle()
        {
            List<MahjongTile> mahjongTiles = new List<MahjongTile>(Parent.GetComponentsInChildren<MahjongTile>(true));
            mahjongTiles.ForEach((t) => { t.SetExcluded(false); });
            List<SpritesPair> spritesPairs = new List<SpritesPair>();

            while (mahjongTiles.Count > 0)   // get the list of match pairs
            {
                var mTile = mahjongTiles[0];
                mahjongTiles.RemoveAt(0);
                MahjongTile pairTile = null;
                foreach (var item in mahjongTiles)
                {
                    if (mTile.SpriteCanMatchhWith(item.MSprite))
                    {
                        pairTile = item;
                        SpritesPair freePaar = new SpritesPair(mTile.MSprite, item.MSprite);
                        spritesPairs.Add(freePaar);
                        break;
                    }
                }
                mahjongTiles.Remove(pairTile);
            }
            Cells.ForEach((c) => { c.DestroyGridObjects(); });
            int tilesCount = spritesPairs.Count * 2;
            SetObjectsData(LcSet, GameMode.Play, tilesCount);
            tiles.ForEach((t) => { t.SetExcluded(false); });
            List<MahjongTile> tT = new List<MahjongTile>(tiles);

            bool failed = false;
            for (int i = 0; i < tT.Count; i += 2)
            {
                List<MahjongTile> freeTiles = GetFreeToFillTiles(tiles, true, true); // reverse sorted
                if (freeTiles.Count == 1)
                {
                    failed = true;
                    break;
                }
                MahjongTile t1 = freeTiles[0];
                MahjongTile t2 = freeTiles[1];
                freeTiles[0].SetExcluded(true);
                freeTiles[1].SetExcluded(true);
                SpritesPair s = spritesPairs[i / 2];
                t1.SetSprite(s.sprite_1);
                t2.SetSprite(s.sprite_2);
            }
            if (failed) Debug.LogError("!!!HARD MIX FAILED!!!");
        }
    }
}