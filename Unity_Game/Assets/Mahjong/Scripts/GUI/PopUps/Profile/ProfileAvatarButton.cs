using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class ProfileAvatarButton : MonoBehaviour
	{
		public Image avatarImage;
		public Image buttonImage;
		public Button button;
		public Sprite checkedSprite;
		public int index;

		#region temp vars
		private Sprite defaultSprite;
		#endregion temp vars


		#region regular
		private void Start()
		{	
			
		}
		#endregion regular

		public void CheckButton(bool check)
        {
			if (defaultSprite == null) defaultSprite = buttonImage.sprite;
			buttonImage.sprite = (check) ? checkedSprite : defaultSprite;
		}
	}
}
