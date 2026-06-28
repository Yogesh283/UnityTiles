using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    public class StartMapGuiController : MonoBehaviour
    {
        #region temp
        private GuiController MGui { get { return GuiController.Instance; } }
        private GameConstructSet GCSet {get{ return GameConstructSet.Instance; } }
        #endregion temp

        public static StartMapGuiController Instance;

        #region regular
        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }
        #endregion regular 

        /// <summary>
        /// Set all interactable as activity
        /// </summary>
        /// <param name="activity"></param>
        public void SetControlActivity(bool activity)
        {
            Button[] buttons = GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = activity;
            }
        }

        #region utils
        public void SetTextString(Text text, string textString)
        {
            if (text)
            {
                text.text = textString;
            }
        }

        public void SetImageSprite(Image image, Sprite sprite)
        {
            if (image)
            {
                image.sprite = sprite;
            }
        }
        #endregion utils
    }
}