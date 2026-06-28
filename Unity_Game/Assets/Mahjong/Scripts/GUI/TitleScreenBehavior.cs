using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Mkey
{
    public class TitleScreenBehavior : MonoBehaviour
    {
        public static bool show = true;

        [SerializeField]
        private GameObject[] childs;

        // Start is called before the first frame update
        void Start()
        {
            if (childs != null)
            {
                foreach (var item in childs)
                {
                    item.SetActive(show);
                }
            }
        }

        public void StartClick()
        {
            foreach (var item in GetComponentsInChildren<Button>())
            {
                item.interactable = false;
            }
            GuiFader_v2 gF = GetComponent<GuiFader_v2>();
            if (!gF) return;
            gF.FadeOut(0, ()=> { show = false; });
        }
    }
}