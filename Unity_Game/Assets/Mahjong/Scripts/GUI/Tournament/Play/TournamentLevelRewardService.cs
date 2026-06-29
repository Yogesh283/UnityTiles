using System.Collections;
using Mkey;
using Mkey.Network;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Grants 50 coins when a tournament mahjong level is completed (once per room match).
    /// </summary>
    public static class TournamentLevelRewardService
    {
        public const int CoinsPerLevel = 50;
        private const string GuardPrefix = "mk_tournament_level_reward_";

        public static void GrantOnLevelComplete()
        {
            if (!TournamentSession.IsActive) return;

            string roomId = TournamentMatchManager.ActiveRoomId;
            if (!TryClaimLevelReward(roomId)) return;

            if (ApiConfig.Current.UseLocalSimulation)
            {
                if (CoinsHolder.Instance)
                    CoinsHolder.Add(CoinsPerLevel);

                LevelCoinRewardEffect.Play(CoinsPerLevel);
                return;
            }

            if (NetworkManager.HasInstance)
                NetworkManager.Instance.StartCoroutine(GrantOnlineRoutine(roomId));
        }

        private static IEnumerator GrantOnlineRoutine(string roomId)
        {
            var task = WalletService.CreditTournamentLevelRewardAsync(roomId, CoinsPerLevel);
            while (!task.IsCompleted)
                yield return null;

            if (!task.Result.Success)
            {
                Debug.LogWarning("[TournamentLevelReward] Server credit failed: " + task.Result.ErrorMessage);
                yield break;
            }

            LevelCoinRewardEffect.Play(CoinsPerLevel);
            if (CoinsHolder.Instance)
                CoinsHolder.Instance.SetCount(task.Result.Data);
        }

        private static bool TryClaimLevelReward(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return false;

            string key = GuardPrefix + roomId;
            if (PlayerPrefs.HasKey(key))
                return false;

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            return true;
        }
    }
}
