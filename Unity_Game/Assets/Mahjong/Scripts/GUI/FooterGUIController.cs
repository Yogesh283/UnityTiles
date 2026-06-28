using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Mkey
{
    public class FooterGUIController : MonoBehaviour
    {
        [SerializeField]
        private GameObject PauseButton;

        #region temp vars
        private GameBoard MBoard => GameBoard.Instance;
        #endregion temp vars

        public static FooterGUIController Instance { get; private set; }

        #region regular
        void Awake()
        {
            if (Instance) Destroy(Instance.gameObject);
            Instance = this;
        }

        private IEnumerator Start()
        {
            while (MBoard == null) yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (GameBoard.GMode == GameMode.Edit)
            {
                gameObject.SetActive(false);
            }
            else
            {
               
            }
        }

        private void OnDestroy()
        {
           
        }
        #endregion regular

        public void SetControlActivity(bool activity)
        {
            Button[] buttons = GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = activity;
            }
        }
    }
}
