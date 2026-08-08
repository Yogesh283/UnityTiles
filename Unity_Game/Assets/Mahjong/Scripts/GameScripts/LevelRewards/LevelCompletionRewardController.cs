using UnityEngine;
using System.Collections;
using Mkey.Network;

namespace Mkey
{
    /// <summary>
    /// Grants 50 tournament coins every time a campaign level is won (levels 1–300).
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class LevelCompletionRewardController : MonoBehaviour
    {
        private static LevelCompletionRewardController instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance) return;

            GameObject host = new GameObject(nameof(LevelCompletionRewardController));
            instance = host.AddComponent<LevelCompletionRewardController>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            GameEvents.WinLevelAction += OnWinLevel;
        }

        private void OnDisable()
        {
            GameEvents.WinLevelAction -= OnWinLevel;
        }

        private void OnWinLevel()
        {
            if (Tournament.TournamentSession.IsActive) return;

            int levelIndex = GameLevelHolder.CurrentLevel;
            StartCoroutine(GrantRewardRoutine(levelIndex));
        }

        private static IEnumerator GrantRewardRoutine(int levelIndex)
        {
            var task = LevelCompletionRewardService.TryGrantRewardAsync(levelIndex);
            while (!task.IsCompleted)
                yield return null;

            var result = task.Result;
            if (!result.Success || result.Data == null)
            {
                // Don't block the victory celebration with an error dialog.
                Debug.LogWarning("LevelCompletionReward: server reward request failed: " +
                                 (result.ErrorMessage ?? "Unknown error"));
                yield break;
            }

            if (!result.Data.rewardGiven)
            {
                Debug.LogWarning(
                    "LevelCompletionReward: server returned reward_given=false for level " +
                    LevelCompletionRewardService.ToLevelNumber(levelIndex) +
                    " (API: " + ApiConfig.Current.ServerRoot + ")");
                yield break;
            }

            if (CoinsHolder.Instance)
                CoinsHolder.Instance.SetCount(result.Data.currentWalletBalance);

            LevelCoinRewardEffect.Play(result.Data.rewardCoins);
            // Celebration UI is on WinPU (Intelligent!); skip the old auto overlay.
        }
    }
}
