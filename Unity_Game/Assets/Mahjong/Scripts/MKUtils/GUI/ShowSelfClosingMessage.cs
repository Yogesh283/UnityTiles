using UnityEngine;
using System.Collections;

/*
    30.10.2020 - first
    01.02.2021 - add  public void ShowSelfClosingMesssage()
 */

namespace Mkey
{
    [CreateAssetMenu]
    public class ShowSelfClosingMessage : ScriptableObject
	{
        [SerializeField]
        private WarningMessController messagePrefab;
        [SerializeField]
        private string caption = "Succesfull!!!";
        [SerializeField]
        private string message = "Product received successfull.";
        [SerializeField]
        private float showTime = 3f;

        #region temp vars
        protected static GuiController mGui;
        #endregion temp vars

        /// <summary>
        /// Show message with close button
        /// </summary>
        public void ShowMesssage()
        {
            if (!mGui) mGui = FindFirstObjectByType<GuiController>();
            if (mGui && messagePrefab)
            {
                mGui.ShowMessageWithYesNoCloseButton(messagePrefab, caption, message, () => { }, null, null);
            }
            else if (mGui)
            {
                mGui.ShowMessageWithYesNoCloseButton(caption, message, () => { }, null, null);
            }
        }

        /// <summary>
        /// Show message without close button
        /// </summary>
        public void ShowSelfClosingMesssage()
        {
            if (!mGui) mGui = FindFirstObjectByType<GuiController>();
            if (mGui && messagePrefab)
            {
                WarningMessController wMC = mGui.ShowMessageWithYesNoCloseButton(messagePrefab, caption, message, null, null, null);
                if (wMC) TweenExt.DelayAction(wMC.gameObject, showTime, ()=> { if(wMC) wMC.CloseWindow(); });
            }
            else if (mGui)
            {
                mGui.ShowMessageWithYesNoCloseButton(caption, message, () => { }, null, null);
            }
        }
    }
}
