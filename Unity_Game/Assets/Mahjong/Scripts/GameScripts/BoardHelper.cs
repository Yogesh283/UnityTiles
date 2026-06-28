using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class BoardHelper : MonoBehaviour
	{ 
		public void RestartLevel()
        {
			if(GameBoard.Instance) GameBoard.Instance.RestartLevel(); 
        }
	}
}
