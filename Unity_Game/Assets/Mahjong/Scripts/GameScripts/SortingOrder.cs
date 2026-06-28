using System.Collections;

using System.Collections.Generic;

using UnityEngine;



namespace Mkey

{

    public static class SortingOrder

    {

        #region sorting orders

        public static int Base

        {

            get

            {

                return 2;

            }

        }

        public static int MahjongTile

        {

            get

            {

                return Base + 5;

            }

        }



        public static int MahjongTileToFront

        {

            get

            {

                return Base + 1000;

            }

        }

        #endregion sorting orders

    }

}


