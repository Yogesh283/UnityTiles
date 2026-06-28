using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class UndoMatch : MonoBehaviour
	{
		[SerializeField]
		private GameObject undoGroup; // gui parent
		[SerializeField]
		private Text undoCounterText;

		#region temp vars
		private int maxCount = 10000;  // maximum number of saves
		private List<GridCell> checkedCells;
		private MatchGrid matchGrid;
		private GameBoard MBoard { get { return GameBoard.Instance; } }
		private GameConstructSet GCSet { get { return GameConstructSet.Instance; } }
		private GameObjectsSet GOSet { get { return GCSet.GOSet; } }
		private List<UndoState> undoStates;
		#endregion temp vars

		#region regular
		private IEnumerator Start()
		{
			if (GameBoard.GMode == GameMode.Play)
			{
				undoStates = new List<UndoState>();
				while (!MBoard) yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();
				matchGrid = MBoard.MainGrid;
				checkedCells = matchGrid.Cells;
				checkedCells.RemoveAll((c)=> {return c.GetGridObjects(true).Length == 0; }); // remove empty cells
				MBoard.BeforeCollectAction += SaveBeforeMatch;
				MBoard.ShuffleGridBeginAction += CleanUndoStates;
				GameEvents.RestartAction += CleanUndoStates;
				SubscribeUndoHolder();
				RefreshGUI();
			}
#if UNITY_EDITOR
			else // edit mode
			{
				if (undoGroup) undoGroup.SetActive(false);
				if (undoCounterText) undoCounterText.text = "";
			}
#endif
		}

		private void OnDestroy()
		{
			if (GameBoard.GMode == GameMode.Play)
			{
				if (MBoard) MBoard.BeforeCollectAction -= SaveBeforeMatch;
				if (MBoard) MBoard.ShuffleGridBeginAction -= CleanUndoStates;
				GameEvents.RestartAction -= CleanUndoStates;
				UnsubscribeUndoHolder();
			}
		}

		private void SubscribeUndoHolder()
		{
			if (!UndoHolder.Instance) return;
			UndoHolder.Instance.ChangeEvent.AddListener(OnUndoCountChanged);
			UndoHolder.Instance.LoadEvent.AddListener(OnUndoCountChanged);
		}

		private void UnsubscribeUndoHolder()
		{
			if (!UndoHolder.Instance) return;
			UndoHolder.Instance.ChangeEvent.RemoveListener(OnUndoCountChanged);
			UndoHolder.Instance.LoadEvent.RemoveListener(OnUndoCountChanged);
		}

		private void OnUndoCountChanged(int _)
		{
			RefreshGUI();
		}
        #endregion regular

        #region handlers
        private void SaveBeforeMatch(GridCell gridCell_1 , GridCell gridCell_2, MahjongTile mahjongTile_1, MahjongTile mahjongTile_2)
		{
			SaveUndoState(new List<GridCell>() { gridCell_1, gridCell_2 });
		}
		#endregion handlers

		/// <summary>
		///  Save Undo state of the match board
		/// </summary>
		private void SaveUndoState(List<GridCell> cells)
		{
            if (undoStates.Count > maxCount)
            {
                undoStates.RemoveAt(0);
            }
            UndoState ds = new UndoState(ScoreHolder.Count, cells);
            undoStates.Add(ds);
            // Debug.Log("save undo state " + undoStates.Count);
			RefreshGUI();
        }

		/// <summary>
		///  Restore Undo state of the match board
		/// </summary>
		public void RestoreUndoState()
		{
			if (GameBoard.GMode == GameMode.Edit) return;
			if (undoStates == null || undoStates.Count == 0) return;
			if (UndoHolder.Instance && UndoHolder.Count <= 0) return;

			UndoState ds = undoStates[undoStates.Count - 1];
			ds.Restore(MBoard.MainGrid, GOSet.mahjongTilePrefab);
			undoStates.RemoveAt(undoStates.Count - 1);
			MBoard.RaiseUndoEvents();

			if (UndoHolder.Instance)
				UndoHolder.Add(-1);

			RefreshGUI();
			GameEvents.UndoAction?.Invoke();
		}

		private void CleanUndoStates()
        {
			undoStates = new List<UndoState>();
			RefreshGUI();
		}

		private void RefreshGUI()
        {
			if (!undoCounterText) return;

			if (UndoHolder.Instance)
				undoCounterText.text = UndoHolder.Count.ToString();
			else if (undoStates != null)
				undoCounterText.text = undoStates.Count.ToString();
		}
	}

	public class UndoState
	{
		public int score;
		public List<GCellObects> cells;
		private GridCell gridCell;

		public UndoState(int score, List<GridCell> checkedCells)
        {
			this.score = score;
			cells = new List<GCellObects>();
			for (int i = 0; i < checkedCells.Count; i++)
			{
				gridCell = checkedCells[i];
				List<GridObjectState> gOSs = GetGridObjectsStates(gridCell);
				cells.Add(new GCellObects(gridCell.Row, gridCell.Column, gOSs));
			}
			// Debug.Log("saved cells: " + cells.Count);
		}

		private List<GridObjectState> GetGridObjectsStates(GridCell gC)
		{
			GridObject[] gridObjects = gC.GetGridObjects(true);
			List<GridObjectState> res = new List<GridObjectState>();
			foreach (var item in gridObjects)
			{
				var objectState = new GridObjectState(item.Layer);
				objectState.SetSpite(item.SRenderer.sprite);
				res.Add(objectState);
			}
			return res;
		}

		public void Restore(MatchGrid matchGrid, GridObject prefab)
        {
			ScoreHolder.Instance.SetCount(score);
            foreach (var objects in cells)
            {
				Debug.Log("restore cell: " + objects.row + " : " + objects.column);
				GridCell gridCell = matchGrid[objects.row, objects.column];
				if(gridCell) RestoreCellState(gridCell, objects, prefab);
            }
		}

		private void RestoreCellState(GridCell gridCell, GCellObects cellObects, GridObject prefab)
        {
            foreach (var item in cellObects.gridObjects)
            {
				GridObject gO = gridCell.GetLayerObject(item.layer, false, true);
				Debug.Log("layer object: " + gO + " ; layer: " + item.layer);
                if (!gO)
                {
					GridObject gridObject = gridCell.SetObject(prefab, item.layer);
					gridObject.SetSprite(item.GetSprite());
					gridObject.SetToFront(false);

				}
			}
        }
	}
}
