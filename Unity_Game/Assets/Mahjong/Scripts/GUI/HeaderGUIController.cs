using UnityEngine;

using UnityEngine.UI;

using System.Collections;



namespace Mkey

{

    public class HeaderGUIController : MonoBehaviour

    {

        #region temp vars

        private GameBoard MBoard => GameBoard.Instance; 

        #endregion temp vars



        public static HeaderGUIController Instance { get; private set; }



        #region regular

        private void Awake()

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


