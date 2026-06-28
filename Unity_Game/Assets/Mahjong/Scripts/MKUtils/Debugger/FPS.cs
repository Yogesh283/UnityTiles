using UnityEngine;
using UnityEngine.UI;
/*
	31.08.2020 - 1.0
 */

namespace Mkey
{
	public class FPS : MonoBehaviour
	{
		[SerializeField]
		private Text fpsText;

		private float deltaTime;

        #region regular
        void Update()
		{
			deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
			SetFPS();
		}
        #endregion regular

        private void SetFPS()
		{
			if (!fpsText) return;
			float msec = deltaTime * 1000.0f;
			float fps = 1.0f / deltaTime;
			fpsText.text = string.Format("FPS: {0:00.} ({1:00.0} ms)", fps, msec);
		}
	}
}