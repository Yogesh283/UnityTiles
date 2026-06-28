using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//https://answers.unity.com/questions/1153163/unity-in-app-purchase-is-there-a-way-to-localize-p.html
//https://forum.unity.com/threads/solved-unity-iap-get-item-price.391448/

/*
 07.06.2023 - first
 */
namespace Mkey
{
	public class PriceLocalize : MonoBehaviour
	{
		public string productID;
		[SerializeField]
		private bool updatePriceText = false;

		#region regular	
		private void Start()
		{
			if (!updatePriceText) return;
			Text t = GetComponent<Text>();
            if (t && !string.IsNullOrEmpty(productID) && Purchaser.Instance)
            {
				string price = Purchaser.Instance.GetProductPriceFromStore(productID);
				if (!string.IsNullOrEmpty(price)) t.text = price;
            }
		}
		#endregion regular
	}
}
