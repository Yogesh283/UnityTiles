using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    public class MahjongThemePU : PopUpsController
    {
        [SerializeField]
        private ThemeButton  themeButtonPrefab;

        [SerializeField]
        private RectTransform buttonsContainer;

        #region temp vars
        private int themeIndex;
        #endregion temp vars

        #region temp vars


        private GameBoard MBoard => GameBoard.Instance;
        private GuiController MGui => GuiController.Instance;
        private GameConstructSet GCSet => GameConstructSet.Instance;
        private GameObjectsSet GOSet => (GCSet) ? GCSet.GOSet : null;
        private GameLevelHolder MGLevel => GameLevelHolder.Instance;
        private CoinsHolder MCoins => CoinsHolder.Instance;
        #endregion temp vars

        #region regular
        private void Start()
        {
            themeIndex = GameThemesHolder.ThemeIndex;
            GameThemesHolder.Instance.ChangeEvent += ChangeThemeHandler;
            CreateButtons();
        }

        private void OnDestroy()
        {
            GameThemesHolder.Instance.ChangeEvent -= ChangeThemeHandler;
        }
        #endregion regular

        private void CreateButtons()
        {
            ThemeButton[] tbs = buttonsContainer.GetComponentsInChildren<ThemeButton>();
            foreach (var item in tbs)
            {
                DestroyImmediate(item.gameObject);
            }
            GameThemesHolder gameThemesHolder = GameThemesHolder.Instance;
            if (GameThemesHolder.Instance.themes.Length == 0) return;

            for (int i = 0; i < gameThemesHolder.themes.Length; i++)
            {
                ThemeSpritesHolder tSH = gameThemesHolder.themes[i];
                ThemeButton t = Instantiate(themeButtonPrefab, buttonsContainer);
                int index = i;
                t.index = index;
                t.CreateThemeIcons(tSH);
                t.button.onClick.AddListener(()=> {
                    themeIndex = index;
                    RefresButtons();
                });
            }
            RefresButtons();
        }

        #region event handlers
        private void RefresButtons()
        {
            ThemeButton[] tButtons = buttonsContainer.GetComponentsInChildren<ThemeButton>();
            int index = themeIndex;
            foreach (var tB in tButtons)
            {
                tB.CheckButton(tB.index == index);
            }
        }

        private void ChangeThemeHandler(int oldIndex, int newIndex)
        {
            GameThemesHolder gameThemesHolder = GameThemesHolder.Instance;
            if(MBoard) MBoard.MainGrid.ReplaceMahjongSprites(gameThemesHolder.themes[oldIndex], gameThemesHolder.themes[newIndex]);
            // RefresButtons();
        }
        #endregion event handlers 

        public void Confirm_Click()
        {
            GameThemesHolder.Instance.SetIndex(themeIndex);
            CloseWindow();
        }
    }
}