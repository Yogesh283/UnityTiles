using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	[DefaultExecutionOrder (-100)]
	public class ScriptableHolder : MonoBehaviour
	{
		public ScriptableObject[] scriptable;
		
		
		#region regular
		private void Awake()
        {
        }
		
		private void Start()
		{	
			
		}
		
		private void OnDestroy()
        {
			
        }
		#endregion regular
	}
}
