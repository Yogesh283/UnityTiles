using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class MatchGridRenderer : MonoBehaviour
	{
#if UNITY_EDITOR
		public SolidLineRenderer solidLineRendererPrefab;
		public GameConstructor gameConstructor;
		private List<SolidLineRenderer> solidLineRenderers;

		public Color color_0;
		public Color color_1;
		public Color color_2;
		public Color color_3;
		public Color color_4;

		#region temp vars
		private Color []colors;
		#endregion temp vars

		#region regular
		private void Awake()
        {
			colors = new Color[] { color_0, color_1, color_2, color_3, color_4 };
		}
		
		private IEnumerator Start()
		{
			yield return null;
			while(!GameBoard.Instance) yield return null;
			solidLineRenderers = new List<SolidLineRenderer>();
			yield return null;

			gameConstructor.ChangeCurrentLayerAction += ShowSpritesGrid;
			gameConstructor.ChangeCurrentLayerAction += EnableConstructColliders;

			gameConstructor.AddTileAction += SetSpriteColor;
			gameConstructor.AddTileAction += EnableConstructCollider;
			gameConstructor.AddTileAction += (go, layer) => { GameBoard.Instance.MainGrid.SetTofrontAll(false);};
			gameConstructor.AddTileAction += ShowSpriteGrid;
			gameConstructor.RemoveTileAction += (layer) => { GameBoard.Instance.MainGrid.SetTofrontAll(false); };
			
			gameConstructor.ReloadBoardAction += SetSpritesColor;
			gameConstructor.ReloadBoardAction += ShowSpritesGrid;

			ShowSpritesGrid();
			EnableConstructColliders(gameConstructor.CurrentLayer);
			SetSpritesColor();
		}
		
		private void OnDestroy()
        {

        }
		#endregion regular

		#region use linerenderer
		/// <summary>
		/// draw grid using linerenderer
		/// </summary>
		/// <param name="gameBoard"></param>
		public void DebugDrawGrid(GameBoard gameBoard)
		{
			DestroyLines();
			MatchGrid matchGrid = GameBoard.Instance.MainGrid;
			Vector3 dPos = matchGrid.Rows[1][1].transform.position - matchGrid.Rows[0][0].transform.position;
			Vector3 dPosH = dPos * 0.5f;

			foreach (var item in matchGrid.Rows)
			{
				Vector3 pos_1 = item[0].transform.position + new Vector3(-dPosH.x, -dPosH.y, 0);
				Vector3 pos_2 = item[item.Length - 1].transform.position + new Vector3(dPosH.x, -dPosH.y, 0); 
				var sLR = solidLineRendererPrefab.Create(gameBoard.transform, pos_1, pos_2);
				solidLineRenderers.Add(sLR);
			}

			foreach (var item in matchGrid.Columns)
			{
				Vector3 pos_1 = item[0].transform.position + new Vector3(dPosH.x, -dPosH.y, 0);
				Vector3 pos_2 = item[item.Length - 1].transform.position + new Vector3(dPosH.x, dPosH.y, 0);
				var sLR = solidLineRendererPrefab.Create(gameBoard.transform, pos_1, pos_2);
				solidLineRenderers.Add(sLR);
			}
		}

		private void DestroyLines()
        {
			foreach (var item in solidLineRenderers)
			{
				if (item)
				{
					item.transform.parent = null;
					Destroy(item.gameObject);
				}
			}
        }
        #endregion use linerenderer

        #region sprite grid
        private void ShowSpritesGrid(int currentLayer)
        {
			MahjongTile[] mahjongTiles = GameBoard.Instance.GetComponentsInChildren<MahjongTile>(true);
            for (int i = 0; i < mahjongTiles.Length; i++)
            {
				if(mahjongTiles[i].Layer == currentLayer && currentLayer >= 0) mahjongTiles[i].ShowConstructLines(true, 0.5f);
                else
                {
					mahjongTiles[i].ShowConstructLines(mahjongTiles[i].Layer == currentLayer - 1 && currentLayer > 0, 1);
				}
			}
        }

		private void ShowSpritesGrid()
		{
			ShowSpritesGrid(gameConstructor.CurrentLayer);
		}

		private void ShowSpriteGrid(GridObject gridObject, int currentLayer)
		{
			MahjongTile mahjongTile = gridObject.GetComponent<MahjongTile>();
			if (mahjongTile.Layer == currentLayer && currentLayer >= 0) mahjongTile.ShowConstructLines(true, 0.5f);
			else mahjongTile.ShowConstructLines(mahjongTile.Layer == currentLayer - 1 && currentLayer > 0, 1);
		}
		#endregion sprite grid

		#region sprite color
		private void SetSpritesColor()
        {
			MahjongTile[] mahjongTiles = GameBoard.Instance.GetComponentsInChildren<MahjongTile>(true);
			for (int i = 0; i < mahjongTiles.Length; i++)
			{
				int layer = mahjongTiles[i].Layer;
				mahjongTiles[i].SetConstructColor(colors[layer]); 
			}
			Debug.Log("set sprites color");
		}

		private void SetSpriteColor(GridObject gridObject, int layer)
		{
			MahjongTile mahjongTile = gridObject.GetComponent<MahjongTile>();
			mahjongTile.SetConstructColor(colors[layer]);
		}
		#endregion sprite color

		#region construct colliders
		private void EnableConstructColliders(int currentLayer)
        {
			MatchGrid matchGrid = GameBoard.Instance.MainGrid;
			bool enableGridColl = currentLayer == 0;

			foreach (var item in matchGrid.Cells)
            {
				item.GetComponent<Collider2D>().enabled = enableGridColl;
            }

			MahjongTile[] mahjongTiles = GameBoard.Instance.GetComponentsInChildren<MahjongTile>(true);
			for (int i = 0; i < mahjongTiles.Length; i++)
			{
				mahjongTiles[i].boxCollider.enabled = ((mahjongTiles[i].Layer == currentLayer - 1 || mahjongTiles[i].Layer == currentLayer) && currentLayer > 0);
			}
		}

		private void EnableConstructCollider(GridObject gridObject, int layer)
		{
			MahjongTile mahjongTile = gridObject.GetComponent<MahjongTile>();
			mahjongTile.boxCollider.enabled = ((mahjongTile.Layer == layer - 1 || mahjongTile.Layer == layer) && layer > 0);
		}
		#endregion construct colliders
#endif
	}
}
