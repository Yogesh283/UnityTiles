using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mkey
{
    public class GridCell : TouchPadMessageTarget
    {
        #region row, column, layer grid
        public MatchGrid MGrid { get; private set; }
        public Column<GridCell> GColumn { get; private set; }
        public Row<GridCell> GRow { get; private set; }
        public int Row { get; private set; }
        public int Column { get; private set; }
        public int Layer { get; private set; }
        public List<Row<GridCell>> Rows { get; private set; }
        #endregion row, column, layer grid

        #region cache fields
        private SpriteRenderer sRenderer;
        #endregion cache fields

        #region events
        public Action<GridCell> GCPointerUpEvent;
        public Action<GridCell> GCPointerDownEvent;
        #endregion events

        #region properties 
        public NeighBors Neighbors { get; private set; }
        public int AddRenderOrder { get { return (GRow != null && GColumn != null) ? (RenderRow * GColumn.Length *1 + (Column*1)) : 0; } }
        private GameBoard MBoard { get { return GameBoard.Instance; } }
        private GameConstructSet GCSet { get { return GameConstructSet.Instance; } }
        private LevelConstructSet LCSet { get { return GCSet.GetLevelConstructSet(GameLevelHolder.CurrentLevel); } }
        private GameObjectsSet GOSet { get { return GCSet.GOSet; } }
        private int RenderRow => Row % 2 == 0 ? Row+0 : Row + 0;
        #endregion properties 

        #region temp
        private GameMode gMode;
        #endregion temp

        #region touchbehavior
        private void PointerDownEventHandler(TouchPadEventArgs tpea)
        {
            if (GameBoard.GMode == GameMode.Play)
            {
                GCPointerDownEvent?.Invoke(this);
            }
            else
            {
                GCPointerDownEvent?.Invoke(this);
            }
        }

        private void PointerUpEventHandler(TouchPadEventArgs tpea)
        {
            if (GameBoard.GMode == GameMode.Play)
            {
                GCPointerUpEvent?.Invoke(this);
            }
        }
        #endregion touchbehavior

        #region set, remove
        internal GridObject SetObject(GridObject prefab, int layer)
        {
            if (!prefab || !prefab.CanSetBySize(this) || !prefab.CanSetByLayer(this, layer)) return null;
            GridObject gO = prefab.Create(this, layer);
            if (gO) 
            {
                // gO.SetLayer(layer);
                sRenderer.enabled = (GameBoard.GMode != GameMode.Play);
                // gO.boxCollider.enabled = (GameBoard.GMode == GameMode.Play);
                // gO.GetComponent<MahjongTile>().ShowConstructLines(GameBoard.GMode == GameMode.Edit);
            }
            return gO;
        }
      
        private void RemoveObject(int layer)
        {
            sRenderer.enabled = (GameBoard.GMode != GameMode.Play);
            GridObject[] gOs = GetComponentsInChildren<GridObject>(true);
            foreach (var gO in gOs)
            {
                if (gO && gO.Layer == layer) { 
                    Debug.Log("remove object layer: " + layer );
                    gO.transform.parent = null; 
                    DestroyImmediate(gO.gameObject); }
            }
        }

        public void RemoveObject(int layer, bool cleanTopLayers)
        {
            if(layer == GameConstructSet.MaxLayersCount - 1)
            {
                RemoveObject(layer);
                return;
            }
            if(!cleanTopLayers)
            {
                RemoveObject(layer);
                return;
            }

            List<GridCell> cellsToClean = new List<GridCell>();
            sRenderer.enabled = (GameBoard.GMode != GameMode.Play);
            GridObject[] gOs = GetComponentsInChildren<GridObject>(true);
            foreach (var gO in gOs)
            {
                if (gO && gO.Layer == layer)
                {
                    Debug.Log("remove object layer : " + layer);
                    cellsToClean.AddRange(gO.GetOccupiedCells());
                    gO.transform.parent = null;
                    DestroyImmediate(gO.gameObject);
                }
            }

            for (int i = layer + 1; i < GameConstructSet.MaxLayersCount; i++)
            {
                List<GridObject> objectsToRemove = new List<GridObject>();
                foreach (var cell in cellsToClean)
                {
                    objectsToRemove.Add (cell.GetLayerObject(i, true, true));
                }

                foreach (var gO in objectsToRemove)
                {
                   if(gO) cellsToClean.AddRange(gO.GetOccupiedCells());
                }
                foreach (var cell in cellsToClean)
                {
                    cell.RemoveObject(i);
                }
            }
        }

        public void UnLinkObject(int layer)
        {
            sRenderer.enabled = (GameBoard.GMode != GameMode.Play);
            GridObject[] gOs = GetComponentsInChildren<GridObject>(true);
            foreach (var gO in gOs)
            {
                if (gO && gO.Layer == layer)
                {
                    gO.transform.parent = null;
                    break;
                }
            }
        }
        #endregion set, remove

        #region grid objects behavior
        /// <summary>
        /// DestroyImeediate Grid Objects
        /// </summary>
        internal void DestroyGridObjects()
        {
            GridObject[] gridObjects = GetGridObjects(true);
            foreach (var item in gridObjects)
            {
                item.transform.parent = null;
                DestroyImmediate(item.gameObject);
            }
        }
        #endregion matchobject behavior

        #region get objects
        public GridObject[] GetGridObjects(bool includeInactive)
        {
            return GetComponentsInChildren<GridObject>(includeInactive);
        }

        public List<GridObjectState> GetGridObjectsStates(bool includeInactive)
        {
            GridObject[] gOs = GetGridObjects(includeInactive);
            List<GridObjectState> res = new List<GridObjectState>();
            foreach (var item in gOs)
            {
                res.Add(new GridObjectState(item.Layer));
            }
            return res;
        }

        private GridObject GetLayerObject(int layer, bool includeInactive)
        {
            GridObject[] gridObjects = GetGridObjects(includeInactive);
            for (int i = 0; i < gridObjects.Length; i++)
            {
                var gO = gridObjects[i];
                if (gO.Layer == layer) return gO;
            }
            return null;
        }

        public GridObject GetLayerObject(int layer, bool andProxy, bool includeInactive)
        {
            if (!andProxy) return GetLayerObject(layer, includeInactive);

            foreach (var item in MGrid.Cells)
            {
                GridObject gridObjectH = item.GetLayerObject(layer, includeInactive);

                if (gridObjectH)
                {
                    List<GridCell> occupiedCells = gridObjectH.GetOccupiedCells();
                    if (occupiedCells.Contains(this)) return gridObjectH;
                }
            }
            return null;
        }

        public T GetObject<T>() where T : GridObject
        {
            return GetComponentInChildren<T>();
        }
        #endregion get objects

        #region check objects
        public bool HaveLayerObject(int layer)
        {
            GridObject g = GetLayerObject(layer, true);
            return g != null;
        }
        #endregion check objects

        #region create init
        /// <summary>
        ///  used by instancing for cache data
        /// </summary>
        internal void Init(int cellRow, int cellColumn, int cellLayer, Column<GridCell> column, Row<GridCell> row, MatchGrid matchGrid, GameMode gMode)
        {
            MGrid = matchGrid;
            Row = cellRow;
            Column = cellColumn;
            Layer = cellLayer;
            GColumn = column;
            GRow = row;
            this.gMode = gMode;
#if UNITY_EDITOR
            name = ToString();
#endif
            sRenderer = GetComponent<SpriteRenderer>();
            sRenderer.enabled = (gMode != GameMode.Play);
            sRenderer.sortingOrder = SortingOrder.Base;
            Neighbors = new NeighBors(this);

            PointerDownEvent = PointerDownEventHandler;
            PointerUpEvent = PointerUpEventHandler;

            GetComponent<Collider2D>().enabled = (gMode != GameMode.Play);
        }

        #endregion create init

        public override string ToString()
        {
            return "cell : [ row: " + Row + " , col: " + Column + "]";
        }
    }
}