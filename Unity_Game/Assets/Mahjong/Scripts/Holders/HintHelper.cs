using UnityEngine;
using UnityEngine.Events;

namespace Mkey
{
	public class HintHelper : MonoBehaviour
	{
		public PopUpsController getFreePU;

		private HintHolder MHints => HintHolder.Instance;
		private GuiController MGui => GuiController.Instance;

		#region events
		[Header("Events")]
		public UnityEvent ApplyHintEvent;
		public UnityEvent<int> ChangeEvent;
		public UnityEvent<int> GetHintsEvent;
		public UnityEvent<int> ConsumptionHintsEvent;
		public UnityEvent<int> LoadEvent;
		public UnityEvent BeginStartEvent;
		public UnityEvent EndStartEvent;
		#endregion events

		#region temp vars
		private int hints;
		#endregion temp vars

		#region regular
		private void Start()
		{
			BeginStartEvent?.Invoke();
			MHints.ChangeEvent.AddListener(ChangeEventHandler);
			MHints.LoadEvent.AddListener(LoadEventHandler);
			LoadEventHandler(HintHolder.Count);
			EndStartEvent?.Invoke();
		}
		
		private void OnDestroy()
        {
			MHints.ChangeEvent.RemoveListener(ChangeEventHandler);
			MHints.LoadEvent.RemoveListener(LoadEventHandler);
		}
		#endregion regular

		private void ChangeEventHandler(int count)
		{
			ChangeEvent?.Invoke(count);

			if (hints < HintHolder.Count)
            {
				GetHintsEvent?.Invoke(HintHolder.Count - hints);
			}
			else if (hints > HintHolder.Count)
            {

				ConsumptionHintsEvent?.Invoke(hints - HintHolder.Count);
			}
			hints = HintHolder.Count;
		}

		private void LoadEventHandler(int count)
		{
			LoadEvent?.Invoke(count);
			hints = HintHolder.Count;
		}

		public void Select_Hint()
		{
            // the hint is already there
            if (GameBoard.Instance.IsAlreadyHint())
            {
				GuiController.Instance.ShowMessage("", "Matching tiles are already selected", 2, null);
				return;
            }
            if (hints > 0)
			{
				GameBoard.Instance.TrySelectHintMatch((good)=> { if (good) HintHolder.Add(-1); });
				ApplyHintEvent?.Invoke();
				GameEvents.ApplyHintAction?.Invoke();
			}
            else
            {
				if (getFreePU) MGui.ShowPopUp(getFreePU);
            }
		}
	}
}
