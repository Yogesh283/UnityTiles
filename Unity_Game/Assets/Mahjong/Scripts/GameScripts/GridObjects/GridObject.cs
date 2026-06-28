using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mkey
{
    public class GridObject : MonoBehaviour
    {
        public SpriteRenderer SRenderer;
        public BoxCollider2D boxCollider;

        #region properties
        public int Layer { get; protected set; }
        public GridCell ParentCell { get; protected set; }
        public bool Excluded { get; protected set; }       // temporary variable needed during mahjong layout

        public string Name { get { return name; } }
        #endregion properties

        #region protected temp vars
        protected GameConstructSet GCSet { get { return GameConstructSet.Instance; } }
        protected LevelConstructSet LCSet { get { return GCSet.GetLevelConstructSet(GameLevelHolder.CurrentLevel); } }
        protected GameObjectsSet GOSet { get { return GCSet.GOSet; } }
        protected SoundMaster MSound { get { return SoundMaster.Instance; } }
        protected GameBoard MBoard { get { return GameBoard.Instance; } }
        protected MatchGrid MGrid { get { return MBoard.MainGrid; } }
        #endregion protected temp vars

        #region common
        public void SetSprite(Sprite nSprite)
        {
            if (SRenderer)
            {
                SRenderer.sprite = nSprite;
            }
        }

        public void DestroyHierCompetitor(GridCell gCell, bool andProxy, bool cleanTopLayers)
        {
            if (!gCell) return;
            if (GetSize() == Vector2.one)   // simple object
            {
                GridObject gO = gCell.GetLayerObject(Layer, andProxy, true);
                if (gO) gCell = gO.ParentCell;
                if (gO && gCell)
                {
                    gCell.RemoveObject(gO.Layer, cleanTopLayers);
                }
            }
            else                            // multicells object
            {
                List<GridCell> gridCells = GetOccupiedCells(gCell);
                gridCells.ApplyAction((gC) =>
                {
                    GridObject gOH = gC.GetLayerObject(Layer, andProxy, true);
                    if (gOH)
                    {
                        GridCell cell = gOH.ParentCell;
                        if (cell)
                        {
                            cell.RemoveObject(gOH.Layer, cleanTopLayers);
                        }
                    }
                });
            }
        }
        #endregion common

        #region virtual

        /// <summary>
        /// Changing the rendering order of an object
        /// </summary>
        /// <param name="set"></param>
        public virtual void SetToFront(bool set)
        {

        }

        public virtual GridObject Create(GridCell parent, int layer)
        {
            return parent ? Instantiate(this, parent.transform) : Instantiate(this);
        }

        /// <summary>
        /// We check whether this object can be placed in a cell of the game board according to its size and the boundaries of the game board
        /// </summary>
        /// <param name="gCell"></param>
        /// <returns></returns>
        public virtual bool CanSetBySize(GridCell gCell)
        {
            return true;
        }

        /// <summary>
        /// We check whether this object can be placed in a cell of the game board according to its layer
        /// </summary>
        /// <param name="gCell"></param>
        /// <returns></returns>
        public virtual bool CanSetByLayer(GridCell gCell, int layer)
        {
            return true;
        }

        /// <summary>
        /// Get occupied size (rows, columns)
        /// </summary>
        /// <returns></returns>
        public virtual Vector2Int GetSize()
        {
            return Vector2Int.one;
        }

        /// <summary>
        /// Returns an list of cells that are occupied or can be occupied by a given object according to its size
        /// </summary>
        /// <param name="gCell"></param>
        /// <returns></returns>
        public virtual List<GridCell> GetOccupiedCells(GridCell gCell)
        {
            List<GridCell> res = new List<GridCell>();
            res.Add(gCell);
            return res;
        }

        /// <summary>
        /// Returns an list of cells that are occupied by a given object according to its size
        /// </summary>
        /// <returns></returns>
        public virtual List<GridCell> GetOccupiedCells()
        {
            return GetOccupiedCells(ParentCell);
        }
        #endregion virtual

        public void SetLayer(int layer)
        {
            Layer = layer;
        }
    }

    [Serializable]
    public class GridObjectState
    {
        public int layer;
       
        public GridObjectState(int layer)
        {
            this.layer = layer;
        }

        #region undo
        private Sprite sprite;
        public void SetSpite(Sprite sprite) // for undo
        {
            this.sprite = sprite;
        }

        public Sprite GetSprite()
        {
            return sprite;
        }

        public bool IsEqualTo(GridObjectState other)
        {
            if (other==null) return false;
            if (layer != other.layer) return false;
            if (other.GetSprite() != sprite) return false;
            return true;
        }
        #endregion undo
    }
}

