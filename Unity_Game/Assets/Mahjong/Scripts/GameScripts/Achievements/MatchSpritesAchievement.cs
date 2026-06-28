using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mkey
{
    public class MatchSpritesAchievement : Achievement
    {
        [SerializeField]
        private Sprite sourceSprite;
        [SerializeField]
        private List<Sprite> spriteAliases;

        #region events
        public Action ChangeThemeAction;
        #endregion events

        #region properties

        #endregion properties

        #region regular
        public override void Load()
        {
            LoadRewardReceived();
            LoadCurrentCount();
            LoadCurrentStage();
            if (sourceSprite) spriteAliases = GameThemesHolder.Instance.GetSpriteAliases(sourceSprite, true);
            GameEvents.MatchSpritesEvent += MatchSpriteEventHandler;
            ChangeCurrentCountEvent += (cc, tc) => { };
            GameThemesHolder.Instance.ChangeEvent += ChangeThemeEventHandler;
        }

        private void OnDestroy()
        {
            GameEvents.MatchSpritesEvent -= MatchSpriteEventHandler;
            GameThemesHolder.Instance.ChangeEvent -= ChangeThemeEventHandler;
        }
        #endregion regular

        public override string GetUniqueName()
        {
            return "matchsprite_" + ((sourceSprite) ? sourceSprite.name : "any");
        }

        public override Sprite GetAchImage()
        {
            if (sourceSprite == null) return null;
            return GameThemesHolder.Instance.GetSpriteAlias(sourceSprite);
        }

        private void MatchSpriteEventHandler(Sprite sprite_1, Sprite sprite_2)
        {
            if (sourceSprite && sprite_1 && sprite_2)
            {
                if (spriteAliases.Contains(sprite_1) || spriteAliases.Contains(sprite_2))
                {
                    IncCurrentCount();
                    return;
                }

                ThemeSpritesHolder currentTheme = GameThemesHolder.Instance.GetTheme();
                ThemeSpritesHolder spriteTheme = GameThemesHolder.Instance.GetSpriteTheme(sourceSprite);
                if (currentTheme != spriteTheme)
                {
                    Dictionary<Sprite, Sprite> dictionary = GameThemesHolder.Instance.GetSpritesDictionary(spriteTheme, currentTheme);
                    if (dictionary[sourceSprite] == sprite_1 || dictionary[sourceSprite] == sprite_2)
                    {
                        IncCurrentCount();
                        return;
                    }
                }
            }
            else if (!sourceSprite) // any match
            {
                IncCurrentCount();
            }
        }

        private void ChangeThemeEventHandler(int oldIndex, int newIndex)
        {
            ChangeThemeAction?.Invoke();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MatchSpritesAchievement))]
    public class MatchSpriteAchievementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            MatchSpritesAchievement t = (MatchSpritesAchievement)target;
            t.DrawInspector();
        }
    }
#endif
}
