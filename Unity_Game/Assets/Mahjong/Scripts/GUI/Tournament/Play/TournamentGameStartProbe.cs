using Mkey.Network;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// TEMPORARY — in-scene game start sync probe. Remove after investigation.
    /// </summary>
    public static class TournamentGameStartProbe
    {
        private const string Tag = "[TournamentGameStart]";

        private static bool _summaryLogged;
        private static float _sceneEnterAt;

        public static void Reset()
        {
            _summaryLogged = false;
            _sceneEnterAt = Time.realtimeSinceStartup;
        }

        public static void LogSceneEnter()
        {
            Reset();
            Debug.Log($"{Tag} === GAME SCENE SYNC PROBE START ===");
        }

        public static void Step(int step, string name, bool pass, string detail = null)
        {
            string verdict = pass ? "PASS" : "FAIL";
            string msg = string.IsNullOrEmpty(detail)
                ? $"{Tag} STEP {step} {name} => {verdict}"
                : $"{Tag} STEP {step} {name} => {verdict} | {detail}";
            if (pass)
                Debug.Log(msg);
            else
                Debug.LogWarning(msg);
        }

        public static void LogSyncPoll(RoomResponseDto room, bool forceStart = false)
        {
            string status = room?.status ?? "null";
            int players = room?.playerCount ?? 0;
            long matchStart = room?.matchStartAtMs ?? 0;

            Step(1, "room status active|starting|locked (post-countdown)",
                status is "active" or "locked" or "starting",
                $"status={status} players={players}");

            Step(2, "TournamentServerClock receives match_start_at_ms",
                TournamentServerClock.HasScheduledStart || matchStart > 0,
                $"scheduled={TournamentServerClock.ScheduledStartMs} api={matchStart}");

            Step(3, "HasScheduledStart",
                TournamentServerClock.HasScheduledStart,
                $"match_start_at_ms={TournamentServerClock.ScheduledStartMs}");

            Step(4, "IsServerStartTimeReached",
                TournamentServerClock.IsServerStartTimeReached(),
                $"secondsUntilStart={TournamentServerClock.SecondsUntilStart():F2}");

            if (forceStart)
                Debug.LogWarning($"{Tag} FORCE START — waiting room countdown already completed");
        }

        public static void LogBeforeBeginRound()
        {
            Debug.Log($"{Tag} Countdown Begin (in-scene gate passed)");
            Step(5, "TournamentGameSessionController calls BeginRound unlock path", true);
        }

        public static void LogAfterBeginRound(bool matchPlaying, bool gameplayRunning, bool boardUnlocked)
        {
            Step(6, "BeginRound -> BeginSynchronizedMatch (room Playing)", matchPlaying,
                $"GameplayRunning={gameplayRunning}");
            Step(7, "Gameplay timer armed (GameplayRunning)", gameplayRunning);
            Step(8, "Board unlocked (SetControlActivity true)", boardUnlocked);
            Step(9, "StopLocalGameplayInstant NOT active",
                !TournamentMatchManager.IsMatchLocked && !TournamentMatchManager.IsMatchResolved);
            Step(10, "GameState path",
                matchPlaying,
                $"Waiting->Countdown(done in lobby)->Playing={matchPlaying}");
            Step(11, "Input enabled (TouchManager via SetControlActivity)", boardUnlocked);
            Step(12, "Mahjong board interaction enabled", boardUnlocked);
            Step(13, "Board freeze flag cleared", boardUnlocked && !TournamentMatchManager.IsWaitingForOpponentSync);
        }

        public static void LogGameStarted(bool timerHudCreated)
        {
            Step(14, "RunMatchStartSequence finished (launched from lobby)", true);
            Step(15, "PlayServerCountdownRoutine completed (lobby)", true);
            Step(16, "LaunchGameFromWaitingRoom left gameplay ready",
                !TournamentMatchManager.IsWaitingForOpponentSync,
                $"waitingForSync={TournamentMatchManager.IsWaitingForOpponentSync}");

            Step(7, "Timer HUD created", timerHudCreated);

            if (_summaryLogged)
                return;

            _summaryLogged = true;
            bool overall = TournamentSession.GameplayRunning &&
                           !TournamentMatchManager.IsWaitingForOpponentSync;
            Debug.Log($"{Tag} === OVERALL GAME START SYNC: {(overall ? "PASS" : "FAIL")} === " +
                      $"(elapsed={Time.realtimeSinceStartup - _sceneEnterAt:F1}s)");
        }

        public static void LogAbort(string reason)
        {
            Debug.LogWarning($"{Tag} ABORT — {reason}");
            if (_summaryLogged)
                return;

            _summaryLogged = true;
            Debug.LogWarning($"{Tag} === OVERALL GAME START SYNC: FAIL === ({reason})");
        }
    }
}
