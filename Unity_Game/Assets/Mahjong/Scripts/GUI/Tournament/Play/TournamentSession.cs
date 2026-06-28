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
            ElapsedSeconds = 0f;
            MoveCount = 0;
            FinalScore = 0;
            GameplayRunning = false;
        }

        public static void BindRoom(string roomId, int levelIndex)
        {
            ActiveRoomId = roomId;
            MatchLevelIndex = levelIndex;
        }

        public static void PrepareGameLevel()
        {
            int level = MatchLevelIndex >= 0 ? MatchLevelIndex : SharedGameLevelIndex;
            GameLevelHolder.CurrentLevel = level;
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
            ElapsedSeconds = 0f;
            MoveCount = 0;
            FinalScore = 0;
            GameplayRunning = false;
        }
    }
}
