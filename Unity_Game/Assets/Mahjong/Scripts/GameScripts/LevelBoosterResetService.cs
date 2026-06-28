using UnityEngine;

namespace Mkey
{
    /// <summary>
    /// Resets Hint, Shuffle, and Undo to 20 uses when the player advances to a new level.
    /// Does not reset on in-level Restart (RestartAction).
    /// </summary>
    public static class LevelBoosterResetService
    {
        public const int UsesPerLevel = 20;

        private static int lastResetLevelIndex = int.MinValue;

        public static void OnLevelStart(int levelIndex)
        {
            if (levelIndex == lastResetLevelIndex)
                return;

            ResetBoosters();
            lastResetLevelIndex = levelIndex;
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
