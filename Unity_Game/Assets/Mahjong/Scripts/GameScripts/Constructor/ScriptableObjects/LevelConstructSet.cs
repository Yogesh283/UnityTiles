using UnityEngine;
using System;
using System.Collections.Generic;

namespace Mkey
{
    public class LevelConstructSet : BaseScriptable
    {
        [HideInInspector]
        [SerializeField]
        private PopUpsController levelStartStoryPage;
        [HideInInspector]
        [SerializeField]
        private PopUpsController levelWinStoryPage;
        [HideInInspector]
        public bool hardFlag;

        [Space(16)]
        [SerializeField]
        private int vertSize = 12;
        [SerializeField]
        private int horSize = 12;
        [HideInInspector]
        [SerializeField]
        private float distX = 0.0f;
        [HideInInspector]
        [SerializeField]
        private float distY = 0.0f;
        [SerializeField]
        private float scale = 0.9f;
        [SerializeField]
        private int backGroundNumber = 0;

        [SerializeField]
        public List<GCellObects> cells;

        [HideInInspector]
        public FillType fillType = FillType.SimpleAndGroups;

        #region properties
        public PopUpsController LevelWinStoryPage { get { return levelWinStoryPage; } }

        public PopUpsController LevelStartStoryPage { get { return levelStartStoryPage; } }

        public int VertSize
        {
            get { return vertSize; }
            set
            {
                if (value < 1) value = 1;
                vertSize = value;
                SetAsDirty();
            }
        }

        public int HorSize
        {
            get { return horSize; }
            set
            {
                if (value < 1) value = 1;
                horSize = value;
                SetAsDirty();
            }
        }


        public float DistX
        {
            get { return distX; }
            set
            {
                distX = RoundToFloat(value, 0.05f);
                SetAsDirty();
            }
        }

        public float DistY
        {
            get { return distY; }
            set
            {
                distY = RoundToFloat(value, 0.05f);
                SetAsDirty();
            }
        }

        public float Scale
        {
            get { return scale; }
            set
            {
                if (value < 0) value = 0;
                scale = RoundToFloat(value, 0.05f);
                SetAsDirty();
            }
        }
        #endregion properties

        public int BackGround
        {
            get { return backGroundNumber; }
        }

        /// <summary>
        /// Remove all non-existent cells data from board
        /// </summary>
        /// <param name="gOS"></param>
        public void Clean()
        {
            if (cells == null) return;
            cells.RemoveAll((c) => { return ((c.column >= horSize) || (c.column < 0) || (c.row >= vertSize) || (c.row < 0)); });
                foreach (var item in cells)
                {
                    if (item.gridObjects != null)
                    {
                        item.gridObjects.RemoveAll((o) => { return o == null; });
                    }
                }

            SetAsDirty();
        }

        public void IncBackGround(int length)
        {
            backGroundNumber++;
            backGroundNumber = (int)Mathf.Repeat(backGroundNumber, length);
            Save();
        }

        public void DecBackGround(int length)
        {
            backGroundNumber--;
            backGroundNumber = (int)Mathf.Repeat(backGroundNumber, length);
            Save();
        }

        private float RoundToFloat(float val, float delta)
        {
            int vi = Mathf.RoundToInt(val / delta);
            return (float)vi * delta;
        }

        internal void SaveObjects(GridCell gC)
        {
            if (cells == null) cells = new();
            cells.RemoveAll((c) => { return ((c.row == gC.Row) && (c.column == gC.Column)); });
            List<GridObjectState> gOSs = gC.GetGridObjectsStates(true);
            if (gOSs.Count > 0) cells.Add(new GCellObects(gC.Row, gC.Column, gOSs));
            SetAsDirty();
        }

        internal void SaveObjects(List<GridCell> gCs)
        {
            if (cells == null) cells = new();
            foreach (var gC in gCs)
            {
                if (gC)
                {
                    cells.RemoveAll((c) => { return ((c.row == gC.Row) && (c.column == gC.Column)); });
                    List<GridObjectState> gOSs = gC.GetGridObjectsStates(true);
                    if (gOSs.Count > 0)
                    {
                        cells.Add(new GCellObects(gC.Row, gC.Column, gOSs));
                    }
                }
            }
            SetAsDirty();
        }

        internal void SetOffset(Vector2Int offset, MatchGrid matchGrid)
        {
            if (cells == null) cells = new();
            foreach (var item in cells)
            {
                int row = item.row + offset.x;
                int column = item.column + offset.y;
                item.row = row;
                item.column = column;
            }
        }
    }
    [Serializable]
    public class GCellObects
    {
        public int row;
        public int column;
        public List<GridObjectState> gridObjects;

        public GCellObects(int row, int column, List<GridObjectState> gridObjects)
        {
            this.row = row;
            this.column = column;
            this.gridObjects = new List<GridObjectState>(gridObjects);
        }
    }
}



