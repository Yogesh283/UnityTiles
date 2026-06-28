using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class ThemeButton : MonoBehaviour
	{
		public Image buttonImage;
		public Button button;
		public Sprite checkedSprite;
		public Text themeName;
		public RectTransform iconsParent;
		public Image iconPrefab;
		public int index = 0;
		public int iconsCount = 5;


		#region temp vars
		private Sprite defaultSprite;
		#endregion temp vars

		#region regular
		private void Start()
		{
			
		}

		private void OnDestroy()
        {
			
        }
		#endregion regular

		public void CreateThemeIcons(ThemeSpritesHolder theme)
        {
			Image[] images = iconsParent.GetComponentsInChildren<Image>();
			foreach (var item in images)
			{
				item.rectTransform.parent = null;
				DestroyImmediate(item.gameObject);
			}

			List<Sprite> sprites = theme.GetSequencedSprites();

			for (int i = 0; i < iconsCount; i++)
			{
				Image im = Instantiate(iconPrefab, iconsParent);
				im.sprite = sprites[i];
			}
			if(themeName) themeName.text = theme.themeName;
		}

		public void CheckButton(bool check)
		{
			if (defaultSprite == null) defaultSprite = buttonImage.sprite;
			buttonImage.sprite = (check) ? checkedSprite : defaultSprite;
		}
	}
}
