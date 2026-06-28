using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mkey
{
    public class ResetGame : MonoBehaviour
    {
        public void ResetProgress()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}