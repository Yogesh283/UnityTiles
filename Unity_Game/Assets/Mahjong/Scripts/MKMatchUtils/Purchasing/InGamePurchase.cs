using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mkey
{
    public class InGamePurchase : MonoBehaviour
    {
        public int price;

        [SerializeField]
        private UnityEvent CompleteEvent;
        [SerializeField]
        private UnityEvent FailedEvent;

        public void Purchase()
        {
            if (Mkey.Network.ApiConfig.Current.UseLocalSimulation)
            {
                if (CoinsHolder.Count >= price)
                {
                    CoinsHolder.Add(-price);
                    CompleteEvent?.Invoke();
                }
                else
                {
                    FailedEvent?.Invoke();
                }
                return;
            }

            FailedEvent?.Invoke();
        }
    }
}