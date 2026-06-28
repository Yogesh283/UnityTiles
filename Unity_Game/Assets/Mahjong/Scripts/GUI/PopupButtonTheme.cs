using System;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Applies Settings-style green / orange button sprites to all popup action buttons.
    /// </summary>
    public static class PopupButtonTheme
    {
        private static PopupButtonThemeData themeData;

        private static PopupButtonThemeData Theme
        {
            get
            {
                if (!themeData)
                    themeData = Resources.Load<PopupButtonThemeData>("PopupTheme/PopupButtonThemeData");
                return themeData;
            }
        }

        public static void Apply(Transform root)
        {
            if (!root || !Theme) return;

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (!button || ShouldSkip(button)) continue;
                ApplyToButton(button);
            }
        }

        private static bool ShouldSkip(Button button)
        {
            string name = button.gameObject.name;
            if (name.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Sound", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Music", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Highlight", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Plus", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Minus", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Avatar", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("ThemeIcon", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.Equals("ImageButton", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Equals("ThemeButton", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool IsOrangeButton(string name)
        {
            return name.IndexOf("Restart", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Shuffle", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Collect", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Next", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Yes", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Confirm", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Save", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("GetFree", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Get", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ApplyToButton(Button button)
        {
            Image image = button.GetComponent<Image>();
            if (!image) return;

            RectTransform rt = button.GetComponent<RectTransform>();
            float width = 0f;
            if (rt)
            {
                width = rt.rect.width;
                if (width <= 0f)
                    width = Mathf.Abs(rt.sizeDelta.x);
            }
            bool orange = IsOrangeButton(button.gameObject.name);
            bool isLong = width >= 420f;
            bool isSmall = width > 0f && width < 260f;

            Sprite normal;
            Sprite pressed;

            if (isLong)
            {
                normal = orange ? Theme.orangeLongNormal : Theme.greenLongNormal;
                pressed = orange ? Theme.orangeLongPressed : Theme.greenLongPressed;
            }
            else if (isSmall)
            {
                normal = Theme.greenSmallNormal;
                pressed = Theme.greenSmallPressed;
            }
            else
            {
                normal = orange ? Theme.orangeNormal : Theme.greenNormal;
                pressed = orange ? Theme.orangePressed : Theme.greenPressed;
            }

            if (!normal) return;

            image.sprite = normal;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;

            button.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = button.spriteState;
            state.highlightedSprite = null;
            state.selectedSprite = null;
            state.disabledSprite = null;
            state.pressedSprite = pressed ? pressed : normal;
            button.spriteState = state;
        }
    }
}
