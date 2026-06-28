using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Prevents duplicate coin grants for the same tournament room (replay / double-claim protection).
    /// </summary>
    public static class TournamentRewardGuard
    {
        private const string KeyPrefix = "mk_tournament_reward_";

        public static bool TryClaimReward(string roomId, int prizeCoins)
        {
            if (prizeCoins <= 0) return true;
            if (string.IsNullOrEmpty(roomId)) return false;

            string key = KeyPrefix + roomId;
            if (PlayerPrefs.HasKey(key))
                return false;

            PlayerPrefs.SetInt(key, prizeCoins);
            PlayerPrefs.Save();
            return true;
        }

        public static bool WasRewardClaimed(string roomId) =>
            !string.IsNullOrEmpty(roomId) && PlayerPrefs.HasKey(KeyPrefix + roomId);
    }
}
