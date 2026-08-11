using UnityEngine;

namespace Mkey
{
    /// <summary>
    /// Resets Hint, Shuffle, and Undo to 1 use each when the player starts a new level/match.
    /// Does not reset on in-level Restart (RestartAction).
    /// </summary>
    public static class LevelBoosterResetService
    {
        public const int UsesPerLevel = 1;

        private static int lastResetLevelIndex = int.MinValue;

        public static void OnLevelStart(int levelIndex)
        {
            if (levelIndex == lastResetLevelIndex)
                return;

            ResetBoosters();
            lastResetLevelIndex = levelIndex;
        }

        /// <summary>Force 1/1/1 for a new match (shell / tournament), even if the level index repeats.</summary>
        public static void ResetBoostersForNewMatch()
        {
            ResetBoosters();
            lastResetLevelIndex = int.MinValue;
        }

        public static void ResetBoosters()
        {
            if (HintHolder.Instance)
                HintHolder.Instance.SetCount(UsesPerLevel);

            if (ShuffleHolder.Instance)
                ShuffleHolder.Instance.SetCount(UsesPerLevel);

            if (UndoHolder.Instance)
                UndoHolder.Instance.SetCount(UsesPerLevel);
        }
    }
}
