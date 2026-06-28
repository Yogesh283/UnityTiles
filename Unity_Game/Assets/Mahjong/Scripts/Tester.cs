using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class Tester : MonoBehaviour
	{
		#region temp vars
		private GameBoard MBoard { get { return GameBoard.Instance; } }
		private GameConstructSet GCSet { get { return GameConstructSet.Instance; } }
		private LevelConstructSet LCSet { get { return GCSet.GetLevelConstructSet(GameLevelHolder.CurrentLevel); } }
		private GameObjectsSet GOSet { get { return GCSet.GOSet; } }
		private TouchManager MTouch { get { return TouchManager.Instance; } }
		#endregion temp vars

		public static Tester Instance;
		
		#region regular
		private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
		
		IEnumerator Start()
		{
			yield return new WaitForEndOfFrame();
			
		}

		private void Update()
		{
			
		}
		
		private void OnDestroy()
        {
			
        }
		#endregion regular

		public void SetNextSprites()
        {
			// MBoard.MainGrid.SetMahjongSpritesStep();
        }

		public void SetSprites()
		{
			// MBoard.MainGrid.SetMahjongSprites();
		}

		public void HighlightFTS() // highlight free to swap tiles
        {
			List<MahjongTile> result = MBoard.MainGrid.GetFreeToMatchTiles();
            foreach (var item in result)
            {
				item.HighlightHint(true);
            }
		}

		public void UnHighlightFTS() // unhighlight free to swap tiles
		{
			List<MahjongTile> result = MBoard.MainGrid.GetFreeToMatchTiles();
			foreach (var item in result)
			{
				item.HighlightHint(false);
			}
		}


		IEnumerator AutoCollectC()
        {
			PossibleMatches possibleMatches = new PossibleMatches(MBoard.MainGrid.GetFreeToMatchTiles());
			yield return new WaitForSeconds(1);
			while (possibleMatches.Count > 0)
			{
				MatchPair freePaar = possibleMatches.GetMatchPair(0);
				freePaar.mahjongTile_1.HighlightHint(true);
				freePaar.mahjongTile_2.HighlightHint(true);
				yield return new WaitForSeconds(0.5f);
				MBoard.FastCollect(freePaar.mahjongTile_1, freePaar.mahjongTile_2);
				yield return new WaitForSeconds(0.1f);
				possibleMatches = new PossibleMatches(MBoard.MainGrid.GetFreeToMatchTiles());
                if (stopFlag)
                {
					stopFlag = false;
					break;
                }
			}
		}
		public void AutoCollect()
        {
			StartCoroutine(AutoCollectC());
        }

		private bool stopFlag = false;
		public void StopAutoCollect()
		{
			stopFlag = true;
		}
	}
}
