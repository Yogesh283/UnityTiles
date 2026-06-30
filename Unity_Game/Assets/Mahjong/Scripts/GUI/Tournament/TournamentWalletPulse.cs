using System.Collections;
using Mkey;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Subtle scale pulse when wallet balance updates on the tournament page.
    /// </summary>
    public class TournamentWalletPulse : MonoBehaviour
    {
        private int lastBalance = -1;
        private Coroutine pulseRoutine;

        public void NotifyBalance(int balance)
        {
            if (lastBalance >= 0 && balance != lastBalance)
            {
                if (pulseRoutine != null)
                    StopCoroutine(pulseRoutine);
                pulseRoutine = StartCoroutine(Pulse());
            }

            lastBalance = balance;
        }

        private IEnumerator Pulse()
        {
            Transform t = transform;
            Vector3 baseScale = Vector3.one;
            float elapsed = 0f;
            const float duration = 0.28f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = 1f + Mathf.Sin(elapsed / duration * Mathf.PI) * 0.06f;
                t.localScale = baseScale * wave;
                yield return null;
            }

            t.localScale = baseScale;
            pulseRoutine = null;
        }
    }
}
