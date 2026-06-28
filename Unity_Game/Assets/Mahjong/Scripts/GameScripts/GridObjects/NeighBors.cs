using System;
using System.Collections.Generic;

namespace Mkey
{
    [Serializable]
    /// <summary>
    /// Get neighborns for gridcell 
    /// </summary>
    public class NeighBors
    {
        public GridCell Main { get; private set; }
        public GridCell Main_1 { get; private set; } 
        public GridCell Main_2 { get; private set; } // top
        public GridCell Main_3 { get; private set; } // top right
        public GridCell Main_4 { get; private set; } // right
        public GridCell Left_1 { get; private set; }
        public GridCell Left_2 { get; private set; }
        public GridCell Right_1 { get; private set; }
        public GridCell Right_2 { get; private set; }
        public GridCell Top_1 { get; private set; }
        public GridCell Top_2 { get; private set; }
        public GridCell Bottom_1 { get; private set; }
        public GridCell Bottom_2 { get; private set; }
        public List<GridCell> Cells { get; private set; }

        /// <summary>
        /// Create NeighBorns  cells
        /// </summary>
        /// <param name="main"></param>
        /// <param name="id"></param>
        public NeighBors(GridCell main)
        {
            Main = main;
            Main_1 = main;
            Main_2 = main.MGrid[main.Row - 1, main.Column];
            Main_3 = main.MGrid[main.Row - 1, main.Column + 1];
            Main_4 = main.MGrid[main.Row, main.Column + 1];

            Left_1 = main.MGrid[main.Row - 1, main.Column - 1]; 
            Left_2 = main.MGrid[main.Row, main.Column - 1];

            Right_1 = main.MGrid[main.Row - 1, main.Column + 2];
            Right_2 = main.MGrid[main.Row, main.Column + 2];

            Top_1 = main.MGrid[main.Row - 2, main.Column];
            Top_2 = main.MGrid[main.Row - 2, main.Column + 1];

            Bottom_1 = main.MGrid[main.Row + 2, main.Column];
            Bottom_2 = main.MGrid[main.Row + 2, main.Column + 1];

            Cells = new List<GridCell>();
            AddToList(Main_1); AddToList(Main_2);
            AddToList(Main_3); AddToList(Main_4);

            AddToList(Left_1); AddToList(Left_2);
            AddToList(Right_1); AddToList(Right_2);

            AddToList(Top_1); AddToList(Top_2);
            AddToList(Bottom_1); AddToList(Bottom_2);
        }

        public bool Contain(GridCell gCell)
        {
            return Cells.Contains(gCell);
        }

        public void AddToList(GridCell gCell)
        {
            if (gCell && !Contain(gCell)) Cells.Add(gCell);
        }

        public override string ToString()
        {
            return ("All cells : " + ToString(Cells));
        }

        public static string ToString(List<GridCell> list)
        {
            string res = "";
            foreach (var item in list)
            {
                res += item.ToString();
            }
            return res;
        }
    }
}