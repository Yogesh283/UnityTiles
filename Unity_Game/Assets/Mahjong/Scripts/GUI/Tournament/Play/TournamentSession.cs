using Mkey;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Active tournament run state (persists across scene loads until result is dismissed).
    /// </summary>
    public static class TournamentSession
    {
        public const int GameSceneIndex = 2;
        public const int TournamentSceneIndex = 3;
        /// <summary>Fallback level when no room level is assigned.</summary>
        public const int SharedGameLevelIndex = 14;

        public static bool IsActive { get; private set; }
        public static TournamentDefinition Tournament { get; private set; }
        public static string ActiveRoomId { get; private set; }
        public static int MatchLevelIndex { get; private set; } = -1;
        public static int RoomSeed { get; private set; }

        public static float ElapsedSeconds { get; private set; }
        public static int MoveCount { get; private set; }
        public static int FinalScore { get; private set; }
        public static bool GameplayRunning { get; private set; }

        public static bool IsDuelMode =>
            IsActive && Tournament != null && Tournament.id == "duel_1v1";

        private static float startTime;

        public static void Begin(TournamentDefinition tournament)
        {
            Tournament = tournament;
            IsActive = tournament != null;
            ActiveRoomId = null;
            MatchLevelIndex = -1;
            RoomSeed = 0;
            ElapsedSeconds = 0f;
            MoveCount = 0;
            FinalScore = 0;
            GameplayRunning = false;
        }

        public static void BindRoom(string roomId, int levelIndex, int roomSeed = 0)
        {
            ActiveRoomId = roomId;
            MatchLevelIndex = levelIndex;
            RoomSeed = roomSeed;
        }

        /// <summary>
        /// Shared RNG for tile faces + placement — both duel clients must use the same room seed.
        /// </summary>
        public static System.Random CreateTileRandom()
        {
            int seed = RoomSeed;
            if (seed == 0 && !string.IsNullOrEmpty(ActiveRoomId))
                seed = TournamentStringHash.Compute(ActiveRoomId);
            if (seed == 0 && MatchLevelIndex >= 0)
                seed = MatchLevelIndex + 1;

            return new System.Random(seed);
        }

        public static void PrepareGameLevel()
        {
            int level = MatchLevelIndex >= 0 ? MatchLevelIndex : SharedGameLevelIndex;
            GameLevelHolder.CurrentLevel = level;
            SyncSharedTheme();
        }

        private static void SyncSharedTheme()
        {
            if (!IsActive || !GameThemesHolder.Instance || GameThemesHolder.Instance.themes == null)
                return;

            int count = GameThemesHolder.Instance.themes.Length;
            if (count <= 0)
                return;

            int themeIndex = RoomSeed != 0
                ? Mathf.Abs(RoomSeed) % count
                : (MatchLevelIndex >= 0 ? MatchLevelIndex % count : 0);

            GameThemesHolder.Instance.SetIndex(themeIndex);
        }

        public static void StartGameplayTracking()
        {
            if (!IsActive) return;
            startTime = Time.time;
            MoveCount = 0;
            GameplayRunning = true;
        }

        public static void RegisterMove()
        {
            if (!GameplayRunning) return;
            MoveCount++;
        }

        public static float GetLiveElapsedSeconds()
        {
            if (!GameplayRunning) return ElapsedSeconds;
            return Time.time - startTime;
        }

        public static void FinishGameplay(int score)
        {
            if (!GameplayRunning) return;
            ElapsedSeconds = Time.time - startTime;
            FinalScore = score;
            GameplayRunning = false;
        }

        public static void StopGameplay()
        {
            GameplayRunning = false;
        }

        public static void Clear()
        {
            IsActive = false;
            Tournament = null;
            ActiveRoomId = null;
            MatchLevelIndex = -1;
            RoomSeed = 0;
            ElapsedSeconds = 0f;
            MoveCount = 0;
            FinalScore = 0;
            GameplayRunning = false;
        }
    }
}
