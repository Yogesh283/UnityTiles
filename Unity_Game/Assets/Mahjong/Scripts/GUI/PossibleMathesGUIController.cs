using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    public class PossibleMathesGUIController : MonoBehaviour
    {
        [SerializeField]
        private GameObject countGroup;

        [SerializeField]
        private Text countText;

        #region temp vars
        private GameBoard MBoard { get { return GameBoard.Instance; } }
        #endregion temp vars

        #region regular
        private IEnumerator Start()
        {
            while (!MBoard)
            {
                yield return new WaitForEndOfFrame();
            }

            if (GameBoard.GMode == GameMode.Play)
            {
                // set event handlers
                MBoard.ChangePossibleMatchesAction += ChangeHandler;
            }
#if UNITY_EDITOR
            else // edit mode
            {
                if (countGroup) countGroup.SetActive(false);
                if (countText) countText.text = "";
            }
#endif
            Refresh();
        }

        private void OnDestroy()
        {
            if(MBoard) MBoard.ChangePossibleMatchesAction -= ChangeHandler;
        }
        #endregion regular

        private void Refresh()
        {
            if (GameBoard.GMode == GameMode.Play)
            {
                if (MBoard && countText) countText.text = GetString(MBoard.GetPossibleMatchesCount());
            }
#if UNITY_EDITOR
            else // edit mode
            {
                if (countGroup) countGroup.SetActive(false);
                if (countText) countText.text = "";
            }
#endif
        }

        private string GetString(int score)
        {
            return  score.ToString();
        }

        #region eventhandlers
        private void ChangeHandler(int count)
        {
            if (!this) return;
            if (countText) countText.text = GetString(count);
        }
        #endregion eventhandlers
    }
}