using UnityEngine;
using UnityEngine.Events;

namespace Mkey
{
	public class CoinsHelper : MonoBehaviour
	{
		private CoinsHolder MCoins => CoinsHolder.Instance;

		public float animDelay;

		#region events
		public UnityEvent<int> ChangeEvent;
		public UnityEvent<int> GetMoneyEvent;
		public UnityEvent<int> PayOutEvent;
		public UnityEvent<int> LoadEvent;
		public UnityEvent<int> AnimatedUpdateEvent;
		public UnityEvent StartAnimatedUpdateEvent;
		public UnityEvent BeginStartEvent;
		public UnityEvent EndStartEvent;
		#endregion events

		#region temp vars
		private int coins;
		private TweenIntValue amountTween;
		#endregion temp vars

		#region regular
		private void Start()
		{
			BeginStartEvent?.Invoke();
			MCoins.ChangeEvent.AddListener(ChangeEventHandler);
			MCoins.LoadEvent.AddListener(LoadEventHandler);
			LoadEventHandler(CoinsHolder.Count);
			amountTween = new TweenIntValue(gameObject, CoinsHolder.Count, 1, 3, true, (b) => { if (this) AnimatedUpdateEvent?.Invoke(b); });
			EndStartEvent?.Invoke();
		}

		private void Update()
		{
			
		}
		
		private void OnDestroy()
        {
			MCoins.ChangeEvent.RemoveListener(ChangeEventHandler);
			MCoins.LoadEvent.RemoveListener(LoadEventHandler);
		}
		#endregion regular

		private void ChangeEventHandler(int count)
		{
			ChangeEvent?.Invoke(count);
			StartAnimatedTween(count);

			if (coins < CoinsHolder.Count)
            {
				GetMoneyEvent?.Invoke(CoinsHolder.Count - coins);
			}
			else if (coins > CoinsHolder.Count)
            {
				
				PayOutEvent?.Invoke(coins - CoinsHolder.Count);
			}
			coins = CoinsHolder.Count;
		}

		private void StartAnimatedTween(int count)
        {
			if (coins < CoinsHolder.Count)
            {
				StartAnimatedUpdateEvent?.Invoke();
			}
			TweenExt.DelayAction(gameObject, animDelay, () => { if (amountTween != null) amountTween.Tween(count, 100); });
		}

		private void LoadEventHandler(int count)
		{
			LoadEvent?.Invoke(count);
			coins = CoinsHolder.Count;
		}
	}
}
