using UnityEngine;
using UnityEngine.Events;

namespace Mkey
{
	public class ShuffleHelper : MonoBehaviour
	{
		public PopUpsController getFreePU;
		private ShuffleHolder MShuffles => ShuffleHolder.Instance;
		private GuiController MGui => GuiController.Instance;

		#region events
		[Header("Events")]
		public UnityEvent ApplyShuffleEvent;
		public UnityEvent<int> ChangeEvent;
		public UnityEvent<int> GetShufflesEvent;
		public UnityEvent<int> ConsumptionShufflesEvent;
		public UnityEvent<int> LoadEvent;
		public UnityEvent BeginStartEvent;
		public UnityEvent EndStartEvent;
		#endregion events

		#region temp vars
		private int shuffles;
		#endregion temp vars

		#region regular
		private void Start()
		{
			BeginStartEvent?.Invoke();
			MShuffles.ChangeEvent.AddListener(ChangeEventHandler);
			MShuffles.LoadEvent.AddListener(LoadEventHandler);
			LoadEventHandler(ShuffleHolder.Count);
			EndStartEvent?.Invoke();
		}
		
		private void OnDestroy()
        {
			MShuffles.ChangeEvent.RemoveListener(ChangeEventHandler);
			MShuffles.LoadEvent.RemoveListener(LoadEventHandler);
		}
		#endregion regular

		private void ChangeEventHandler(int count)
		{
			ChangeEvent?.Invoke(count);

			if (shuffles < ShuffleHolder.Count)
            {
				GetShufflesEvent?.Invoke(ShuffleHolder.Count - shuffles);
			}
			else if (shuffles > ShuffleHolder.Count)
            {

				ConsumptionShufflesEvent?.Invoke(shuffles - ShuffleHolder.Count);
			}
			shuffles = ShuffleHolder.Count;
		}

		private void LoadEventHandler(int count)
		{
			LoadEvent?.Invoke(count);
			shuffles = ShuffleHolder.Count;
		}

		public void Shuffle_click()
		{
            if (shuffles > 0)
			{
				GameBoard.Instance.ShuffleGrid(null);
				ShuffleHolder.Add(-1);
				ApplyShuffleEvent?.Invoke();
				GameEvents.ApplyShuffleAction?.Invoke();
			}
            else
            {
				if (getFreePU) MGui.ShowPopUp(getFreePU);
			}
		}
	}
}
