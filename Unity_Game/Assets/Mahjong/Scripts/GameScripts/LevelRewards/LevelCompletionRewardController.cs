using UnityEngine;
using System.Collections;

namespace Mkey
{
    /// <summary>
    /// Listens for level wins and grants one-time coin rewards (levels 1–300).
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
                Debug.LogWarning("LevelCompletionReward: server reward request failed: " + result.ErrorMessage);
                yield break;
            }

            if (!result.Data.rewardGiven)
                yield break;

            if (CoinsHolder.Instance)
                CoinsHolder.Instance.SetCount(result.Data.currentWalletBalance);

            LevelCoinRewardEffect.Play(result.Data.rewardCoins);
            ShowRewardPopup(result.Data.currentWalletBalance, result.Data.rewardCoins);
        }

        private static void ShowRewardPopup(int balance, int rewardCoins)
        {
            string body =
                $"🪙 +{rewardCoins} Tournament Coins\n\n" +
                $"Current Tournament Balance: {balance:N0} Coins";

            AppMessageDialog.Show("🎉 LEVEL COMPLETE!", body);
        }
    }
}
