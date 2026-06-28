using UnityEngine;
using UnityEngine.Events;

namespace Mkey
{
	public class GameLevelHelper : MonoBehaviour
	{
		private GameLevelHolder MGLevel => GameLevelHolder.Instance;

		#region events
		public UnityEvent<int> ChangePassedEvent;
		public UnityEvent<int> LoadEvent;
		public UnityEvent<int> PassLevelEvent;
		public UnityEvent<int> StartLevelEvent;

		public UnityEvent BeginStartEvent;
		public UnityEvent EndStartEvent;

		public UnityEvent<int> UpdateGuiLevelNumberEvent;
		#endregion events

		#region temp vars
		private int currentLevel;
		#endregion temp vars

		#region regular
		private void Start()
		{
			BeginStartEvent?.Invoke();

			MGLevel.ChangePassedEvent.AddListener(ChangePassedEventHandler);
			MGLevel.LoadEvent.AddListener(LoadEventHandler);
			MGLevel.StartLevelEvent.AddListener(StartLevelHandler);
			MGLevel.PassLevelEvent.AddListener(PassLevelHandler);

			LoadEventHandler(GameLevelHolder.CurrentLevel);
			EndStartEvent?.Invoke();
		}
		
		private void OnDestroy()
        {
			MGLevel.ChangePassedEvent.RemoveListener(ChangePassedEventHandler);
			MGLevel.LoadEvent.RemoveListener(LoadEventHandler);
			MGLevel.StartLevelEvent.RemoveListener(StartLevelHandler);
			MGLevel.PassLevelEvent.RemoveListener(PassLevelHandler);
		}
		#endregion regular

		private void ChangePassedEventHandler(int number)
		{
			ChangePassedEvent?.Invoke(number);
		}

		private void LoadEventHandler(int number)
		{
			LoadEvent?.Invoke(number);
			UpdateGuiLevelNumberEvent?.Invoke(number + 1);
			currentLevel = GameLevelHolder.CurrentLevel;
		}

		private void StartLevelHandler(int number)
		{
			StartLevelEvent?.Invoke(number);
		}

		private void PassLevelHandler(int number)
		{
			PassLevelEvent?.Invoke(number);
		}
	}
}
