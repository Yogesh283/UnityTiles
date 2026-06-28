using UnityEngine;

namespace Mkey
{
    /// <summary>
    /// Grants 50 coins once per level (1–300) on first successful completion.
    /// </summary>
    public static class LevelCompletionRewardService
    {
        public const int CoinsPerLevel = 50;
        public const int MaxLevel = 300;
        private const string SaveKeyPrefix = "mk_level_coin_reward_";

        public static int ToLevelNumber(int levelIndex) => levelIndex + 1;

        public static bool IsEligibleLevel(int levelNumber) =>
            levelNumber >= 1 && levelNumber <= MaxLevel;

        public static bool HasClaimedReward(int levelNumber)
        {
            if (!IsEligibleLevel(levelNumber)) return true;
            return PlayerPrefs.GetInt(SaveKeyPrefix + levelNumber, 0) == 1;
        }

        /// <summary>
        /// Grants coins for a first-time level win. Returns false if already claimed or ineligible.
        /// </summary>
        public static bool TryGrantReward(int levelIndex, out int levelNumber, out int newBalance)
        {
            levelNumber = ToLevelNumber(levelIndex);
            newBalance = CoinsHolder.Instance ? CoinsHolder.Count : 0;

            if (!IsEligibleLevel(levelNumber)) return false;
            if (HasClaimedReward(levelNumber)) return false;
            if (!CoinsHolder.Instance) return false;

            if (Mkey.Network.ApiConfig.Current.UseLocalSimulation)
            {
                PlayerPrefs.SetInt(SaveKeyPrefix + levelNumber, 1);
                PlayerPrefs.Save();
                CoinsHolder.Add(CoinsPerLevel);
                newBalance = CoinsHolder.Count;
                return true;
            }

            // Online mode: level coin rewards must be granted by server (not implemented locally).
            return false;
        }
    }
}
