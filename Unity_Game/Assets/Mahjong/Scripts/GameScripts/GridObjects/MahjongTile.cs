using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mkey
{
    public class MahjongTile : GridObject
    {
        public Vector3 layerOffset;
        public Sprite MSprite => SRenderer.sprite;

        public GameObject selectPrefab;
        public GameObject hintPrefab;
        public SpriteRenderer leftBorder;
        public SpriteRenderer shadow;
        public SpriteRenderer constructLineHor;
        public SpriteRenderer constructLineVert;
        public TextMesh constructLayerText;

        internal List<GridCell> rawOverBlockers; // left, right an over cells with blockers
        internal List<GridCell> rawLeftBlockers;
        internal List<GridCell> rawRightBlockers;

        #region temp vars
        private GameObject hintObject;
        private GameObject selectObject;
        private Vector3 spriteTransformPosition;
        private bool highlightedHint = false;
        private bool highlightedSel = false;
        #endregion temp vars

        #region properties
        public int OccupiedCols => 2;
        public int OccupiedRows => 2;
        private TouchManager TouchM { get { return TouchManager.Instance; } }
        public Transform spriteTransform => SRenderer.transform;
        #endregion properties

        #region override
        public override void SetToFront(bool toFront)
        {
            SRenderer.sortingOrder = GetRenderOrder(toFront);
            if (shadow) shadow.sortingOrder = (toFront) ? SortingOrder.Base + Layer * 2000 : SortingOrder.Base + Layer * 2000;
            // set border
            if (toFront)
            {
                leftBorder.enabled = false;
            }
            else
            {
                EnableLeftBorder();
            }
        }

        public override string ToString()
        {
            return "MahjongTile: " + Layer;
        }

        public override GridObject Create(GridCell parent, int layer)
        {
            if (!parent) return null;
            Layer = layer;
            DestroyHierCompetitor(parent, true, true);
           
            MahjongTile gridObject = Instantiate(this, parent.transform);
            if (!gridObject) return null;
            gridObject.SetLayer(layer);
            gridObject.transform.localScale = Vector3.one;
            gridObject.transform.localPosition = Vector3.zero + layerOffset * layer;
            gridObject.ParentCell = parent;
#if UNITY_EDITOR
            gridObject.name = "MahjongTile " + parent.ToString();
#endif
            // Debug.Log("create gameobject: " + parent + "; " + gridObject);

            return gridObject;
        }

        public void LinkToCell(GridCell gridCell, bool setPosition)
        {
            transform.parent = gridCell.transform;
            if(setPosition) transform.localPosition = Vector3.zero + layerOffset * Layer;
            ParentCell = gridCell;
#if UNITY_EDITOR
            name = "MahjongTile " + ParentCell.ToString();
#endif
        }

        public override bool CanSetBySize(GridCell gCell)
        {
            List<GridCell> cells = GetOccupiedCells(gCell);
            return (cells.Count == OccupiedCols * OccupiedRows);
        }

        /// <summary>
        /// We check whether this object can be placed in a cell of the game board according to its layer
        /// the object cannot hang over empty cells of the previous layer (this is needed for layers above layer 0)
        /// </summary>
        /// <param name="gCell"></param>
        /// <returns></returns>
        public override bool CanSetByLayer(GridCell gCell, int layer)
        {
            if (layer == 0) return true;
            if (layer > 0)
            {
                int underLayer = layer - 1;
                List<GridCell> cells = GetOccupiedCells(gCell);
                foreach (var item in cells)
                {
                    GridObject gO = item.GetLayerObject(underLayer, true, true);
                    if (!gO) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get occupied size (rows, columns)
        /// </summary>
        /// <returns></returns>
        public override Vector2Int GetSize()
        {
            return new Vector2Int(OccupiedRows, OccupiedCols);
        }

        public override List<GridCell> GetOccupiedCells(GridCell gCell)
        {
            List<GridCell> res = new List<GridCell>();
            int cRow = gCell.Row;
            int cCol = gCell.Column;
            MatchGrid mGrid = gCell.MGrid;
            GridCell _gCell;
            for (int r = cRow; r > cRow - OccupiedRows; r--)
            {
                for (int c = cCol; c < cCol + OccupiedCols; c++)
                {
                    _gCell = mGrid[r, c];
                    if (_gCell) res.Add(_gCell);
                }
            }
            return res;
        }
        #endregion override

        public bool IsFreeToFill()
        {
            NeighBors neighBors = ParentCell.Neighbors;
            // over
            GridObject o1 = (neighBors.Main_1) ? neighBors.Main_1.GetLayerObject(Layer + 1, true, true) : null;
            GridObject o2 = (neighBors.Main_2) ? neighBors.Main_2.GetLayerObject(Layer + 1, true, true) : null;
            GridObject o3 = (neighBors.Main_3) ? neighBors.Main_3.GetLayerObject(Layer + 1, true, true) : null;
            GridObject o4 = (neighBors.Main_4) ? neighBors.Main_4.GetLayerObject(Layer + 1, true, true) : null;
            bool overBlocked1 = (o1 != null && !o1.Excluded);
            bool overBlocked2 = (o2 != null && !o2.Excluded);
            bool overBlocked3 = (o3 != null && !o3.Excluded);
            bool overBlocked4 = (o4 != null && !o4.Excluded);
            bool overBlocked = (overBlocked1 || overBlocked2 || overBlocked3 || overBlocked4);
            if (overBlocked) return false;

            // left
            GridObject l1 = (neighBors.Left_1) ? neighBors.Left_1.GetLayerObject(Layer, true, true) : null;
            GridObject l2  = (neighBors.Left_2) ? neighBors.Left_2.GetLayerObject(Layer, true, true) : null;
            bool leftBlocked1 = (l1 != null && !l1.Excluded);
            bool leftBlocked2 = (l2 != null && !l2.Excluded);
            bool leftBlocked = leftBlocked1 || leftBlocked2;
            if (!leftBlocked) return true;

            // right
            GridObject r1 = (neighBors.Right_1) ? neighBors.Right_1.GetLayerObject(Layer, true, true) : null;
            GridObject r2 = (neighBors.Right_2) ? neighBors.Right_2.GetLayerObject(Layer, true, true) : null;
            bool rightBlocked1 = (r1 != null && !r1.Excluded);
            bool rightBlocked2 = (r2 != null && !r2.Excluded);
            bool rightBlocked = rightBlocked1 || rightBlocked2;
            if (!rightBlocked) return true;

            return false;
        }

        public bool IsFreeToMatch_old()
        {
            NeighBors neighBors = base.ParentCell.Neighbors;
            // over
            GridObject o1 = (neighBors.Main_1) ? neighBors.Main_1.GetLayerObject(Layer + 1, true, true) : null;
            GridObject o2 = (neighBors.Main_2) ? neighBors.Main_2.GetLayerObject(Layer + 1, true, true) : null;
            GridObject o3 = (neighBors.Main_3) ? neighBors.Main_3.GetLayerObject(Layer + 1, true, true) : null;
            GridObject o4 = (neighBors.Main_4) ? neighBors.Main_4.GetLayerObject(Layer + 1, true, true) : null;
            bool overBlocked1 = (o1 != null);
            bool overBlocked2 = (o2 != null);
            bool overBlocked3 = (o3 != null);
            bool overBlocked4 = (o4 != null);
            bool overBlocked = (overBlocked1 || overBlocked2 || overBlocked3 || overBlocked4);
            if (overBlocked) return false;

            // left
            GridObject l1 = (neighBors.Left_1) ? neighBors.Left_1.GetLayerObject(Layer, true, true) : null;
            GridObject l2 = (neighBors.Left_2) ? neighBors.Left_2.GetLayerObject(Layer, true, true) : null;
            bool leftBlocked1 = (l1 != null);
            bool leftBlocked2 = (l2 != null);
            bool leftBlocked = leftBlocked1 || leftBlocked2;
            if (!leftBlocked) return true;

            // right
            GridObject r1 = (neighBors.Right_1) ? neighBors.Right_1.GetLayerObject(Layer, true, true) : null;
            GridObject r2 = (neighBors.Right_2) ? neighBors.Right_2.GetLayerObject(Layer, true, true) : null;
            bool rightBlocked1 = (r1 != null);
            bool rightBlocked2 = (r2 != null);
            bool rightBlocked = rightBlocked1 || rightBlocked2;
            if (!rightBlocked) return true;

            return false;
        }

        public bool IsFreeToMatch()
        {
            // use cached raw blockers
            GridObject bl = null;
            //check over blocked
            foreach (var gCell in rawOverBlockers)
            {
                bl = gCell.GetLayerObject(Layer + 1, false, true);
                if (bl) return false;
            }

            // check left blocked
            bl = null;
            foreach (var gCell in rawLeftBlockers)
            {
                bl = gCell.GetLayerObject(Layer, false, true);
                if (bl) break;
            }
            if (!bl) return true;

            // check right blocked
            GridObject blr = null;
            foreach (var gCell in rawRightBlockers)
            {
                blr = gCell.GetLayerObject(Layer, false, true);
                if (blr) break;
            }
            if (!blr) return true;

            return false;
        }

        #region cache raw blockers
        private List<GridCell> GetOverBlockers()
        {
            NeighBors neighBors = ParentCell.Neighbors;
            List<GridCell> blockers = new List<GridCell>();
            GridObject bl = (neighBors.Main_1) ? neighBors.Main_1.GetLayerObject(Layer + 1, true, true) : null;
            if (bl) blockers.Add(bl.ParentCell);
            bl = (neighBors.Main_2) ? neighBors.Main_2.GetLayerObject(Layer + 1, true, true) : null;
            if (bl) blockers.Add(bl.ParentCell);
            bl = (neighBors.Main_3) ? neighBors.Main_3.GetLayerObject(Layer + 1, true, true) : null;
            if (bl) blockers.Add(bl.ParentCell);
            bl = (neighBors.Main_4) ? neighBors.Main_4.GetLayerObject(Layer + 1, true, true) : null;
            if (bl) blockers.Add(bl.ParentCell);
            return blockers;
        }

        private List<GridCell> GetLeftBlockers()
        {
            NeighBors neighBors = ParentCell.Neighbors;
            List<GridCell> blockers = new List<GridCell>();
            GridObject bl = (neighBors.Left_1) ? neighBors.Left_1.GetLayerObject(Layer, true, true) : null;
            if (bl) blockers.Add(bl.ParentCell);
            bl = (neighBors.Left_2) ? neighBors.Left_2.GetLayerObject(Layer, true, true) : null;
            if (bl) blockers.Add(bl.ParentCell);
            return blockers;
        }

        private List<GridCell> GetRightBlockers()
        {
            NeighBors neighBors = ParentCell.Neighbors;
            List<GridCell> blockers = new List<GridCell>();
            GridObject bl = (neighBors.Right_1) ? neighBors.Right_1.GetLayerObject(Layer, true, true) : null;
            if (bl) blockers.Add(bl.ParentCell);
            bl = (neighBors.Right_2) ? neighBors.Right_2.GetLayerObject(Layer, true, true) : null;
            if (bl) blockers.Add(bl.ParentCell);
            return blockers;
        }

        public void CacheRawBlockers()
        {
            rawOverBlockers = GetOverBlockers();
            rawLeftBlockers = GetLeftBlockers();
            rawRightBlockers = GetRightBlockers();
        }
        #endregion cache raw blockers

        public MahjongTile GetBottomLeftBlocker()
        {
            NeighBors neighBors = ParentCell.Neighbors;
            GridObject l1 = (neighBors.Left_1) ? neighBors.Left_1.GetLayerObject(Layer, true, true) : null;
            GridObject l2 = (neighBors.Left_2) ? neighBors.Left_2.GetLayerObject(Layer, true, true) : null;
            if (l2 && (l1 != l2)) return l2.GetComponent<MahjongTile>();
            return null;
        }

        /// <summary>
        /// returns the object that occupied this cell
        /// </summary>
        /// <param name="matchGrid"></param>
        /// <param name="gridCell"></param>
        /// <returns></returns>
        public static MahjongTile GetOccupied(MatchGrid matchGrid, GridCell gridCell)
        {
            MahjongTile[] source = matchGrid.Parent.GetComponentsInChildren<MahjongTile>();

            foreach (var item in source)
            {
                List<GridCell> occupiedCells = item.GetOccupiedCells();
                if (occupiedCells.Contains(gridCell)) return item;
            }
            return null;
        }

        /// <summary>
        /// exclude object from calculations
        /// </summary>
        /// <param name="exclude"></param>
        public void SetExcluded(bool exclude)
        {
            if(exclude != Excluded)
            {
                Excluded = exclude;
            }
        }

        #region highlight
        public void HighlightHint(bool highlight)
        {
            if (highlightedHint == highlight) return;
            bool useColor = (hintPrefab== null);
            if (useColor) 
            {
                if (highlight)
                {
                    SRenderer.color = new Color(1f, 0.856f, 0.504f);
                    leftBorder.color = new Color(1f, 0.856f, 0.504f);
                }
                else
                {
                    SRenderer.color = new Color(1f, 1f, 1f);
                    leftBorder.color = new Color(1f, 1f, 1f);
                }
            }
            else // use prefab
            {
                if (highlight)
                {
                    if (!hintObject)
                    {
                        hintObject = Instantiate(hintPrefab, SRenderer.transform);
                        hintObject.GetComponent<SpriteRenderer>().sortingOrder = GetRenderOrder(true) + 2;
                    }
                }
                else
                {
                    GameObject old = hintObject;
                    if(old) Destroy(old);
                }
            }
            highlightedHint = highlight;
        }

        public void HighlightSelected(bool highlight)
        {
            if (highlightedSel == highlight) return;
            if (!SRenderer) return;
            bool useColor = (hintPrefab == null);
            if (useColor)
            {
                if (highlight)
                {
                    SRenderer.color = new Color(1f, 0.856f, 0.504f);
                    leftBorder.color = new Color(1f, 0.856f, 0.504f);
                }
                else
                {
                    SRenderer.color = new Color(1f, 1f, 1f);
                    leftBorder.color = new Color(1f, 1f, 1f);
                }
            }
            else // use prefab
            {
                if (highlight)
                {
                    if (!selectObject)
                    {
                        selectObject = Instantiate(selectPrefab, SRenderer.transform);
                        selectObject.GetComponent<SpriteRenderer>().sortingOrder = GetRenderOrder(true) + 1;
                    }
                }
                else
                {
                    GameObject old = selectObject;
                    if (old) Destroy(old);
                }
            }
            highlightedSel = highlight;
        }

        internal void SetFreeHiglightColor(bool highLight)
        {
            SRenderer.color = highLight ? Color.white : new Color(0.7f, 0.8f, 0.7f, 1);
            if (leftBorder) leftBorder.color = highLight ? Color.white : new Color(0.7f, 0.8f, 0.7f, 1);
        }
        #endregion highlight

        public bool SpriteCanMatchhWith(Sprite other)
        {
            return MSprite == other || GOSet.IsOneGroup(MSprite, other);
        }

        private int GetRenderOrder(bool onFront)
        {
            int layerOrder =(onFront)? 20000 : Layer * 2000;

            int addOrder = (ParentCell) ? ParentCell.AddRenderOrder : 0;

            if (onFront)
               return SortingOrder.MahjongTileToFront + addOrder + layerOrder;
            else
               return SortingOrder.MahjongTile + addOrder + layerOrder;
        }

        private void EnableLeftBorder()
        {
            MahjongTile bL = GetBottomLeftBlocker();
            if(bL)
            {
                int rO = bL.GetRenderOrder(false);
                leftBorder.sortingOrder = rO + 1;
                leftBorder.enabled = true;
            }
            else
            {
                leftBorder.enabled = false;
            }
        }

        #region mix
        internal void MixJump(Vector3 toPosition, Action completeCallBack)
        {
            if (hintObject) HighlightHint(false);
            if (selectObject) HighlightSelected(false);
            spriteTransformPosition = spriteTransform.position;
            SimpleTween.Move(SRenderer.gameObject, SRenderer.transform.position, toPosition, 0.5f).AddCompleteCallBack(() =>
            {
                completeCallBack?.Invoke();
            }).SetEase(EaseAnim.EaseInSine);
        }

        internal void ReversMixJump(Action completeCallBack)
        {
            SimpleTween.Move(SRenderer.gameObject, SRenderer.transform.position, spriteTransformPosition, 0.5f).AddCompleteCallBack(() =>
            {
                completeCallBack?.Invoke();
            }).SetEase(EaseAnim.EaseInSine);
        }
        #endregion mix

        #region constructor
        public void ShowConstructLines(bool show, float alpha)
        {
            int renderOrder = GetRenderOrder(false);
            int renderOrder_L = leftBorder.enabled ? leftBorder.sortingOrder : renderOrder;
            renderOrder = (renderOrder_L > renderOrder) ? renderOrder_L + 2 : renderOrder + 2;

            if (constructLineHor) 
            { 
                constructLineHor.gameObject.SetActive(show);
                constructLineHor.sortingOrder = renderOrder;
                constructLineHor.color = new Color(constructLineHor.color.r, constructLineHor.color.g, constructLineHor.color.b, alpha);
            }
            if (constructLineVert) 
            { 
                constructLineVert.gameObject.SetActive(show);
                constructLineVert.sortingOrder = renderOrder;
                constructLineVert.color = new Color(constructLineHor.color.r, constructLineHor.color.g, constructLineHor.color.b, alpha);
            }

            if (constructLayerText)
            {
                constructLayerText.gameObject.SetActive(show);
                constructLayerText.text = (Layer + 1).ToString();
                constructLayerText.color = new Color(constructLayerText.color.r, constructLayerText.color.g, constructLayerText.color.b, alpha);
                SpriteText rend = constructLayerText.GetComponent<SpriteText>();
                if (rend) 
                { 
                    rend.SortingOrder = renderOrder;
                }
            }
        }

        public void SetConstructColor(Color color)
        {
            SRenderer.color = color;
            leftBorder.color = color;
        }
        #endregion constructor
    }
}
