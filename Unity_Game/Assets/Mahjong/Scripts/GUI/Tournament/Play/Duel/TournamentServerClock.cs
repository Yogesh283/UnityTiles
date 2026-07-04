using System;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Room-authoritative clock synced to backend match_start_at_ms (same for all players).
    /// </summary>
    public static class TournamentServerClock
    {
        /// <summary>
        /// Extra delay after <c>match_start_at_ms</c> before gameplay unfreezes. Both duel clients
        /// launch the game scene at (roughly) the same server instant (the waiting room waits for
        /// match_start_at_ms), then need a shared FUTURE anchor to unfreeze together after the scene
        /// finishes loading. This buffer absorbs per-device scene-load variance so both boards go
        /// live at the same server timestamp instead of "whenever my scene finished loading".
        /// </summary>
        public const long GameplayStartOffsetMs = 2500;

        private static double epochRealtime;
        private static bool running;
        private static long scheduledStartMs;
        private static long clockOffsetMs;

        public static bool IsRunning => running;

        public static long ScheduledStartMs => scheduledStartMs;

        /// <summary>Shared server timestamp when gameplay must go live (match_start_at_ms + buffer).</summary>
        public static long GameplayStartMs => scheduledStartMs > 0 ? scheduledStartMs + GameplayStartOffsetMs : 0;

        /// <summary>Align local clock to server time from the latest room payload.</summary>
        public static void SyncServerTime(long serverNowMs)
        {
            if (serverNowMs <= 0) return;
            clockOffsetMs = serverNowMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            TournamentTransitionProbe.LogClockState("SyncServerTime");
        }

        public static void ScheduleServerStart(long matchStartAtMs)
        {
            if (matchStartAtMs <= 0) return;
            scheduledStartMs = matchStartAtMs;
            TournamentTransitionProbe.LogClockState("ScheduleServerStart");
        }

        public static bool HasScheduledStart => scheduledStartMs > 0;

        public static bool IsStartTimeReached()
        {
            if (scheduledStartMs <= 0)
                return true;
            return ServerNowMs >= scheduledStartMs;
        }

        /// <summary>Online instant duel: false until server sets match_start_at_ms.</summary>
        public static bool IsServerStartTimeReached()
        {
            if (scheduledStartMs <= 0)
                return false;
            return ServerNowMs >= scheduledStartMs;
        }

        public static double SecondsUntilStart()
        {
            if (scheduledStartMs <= 0)
                return 0d;
            return Math.Max(0d, (scheduledStartMs - ServerNowMs) / 1000d);
        }

        /// <summary>
        /// Server-authoritative gameplay-unfreeze gate for duel clients. Returns false until the
        /// shared timestamp (match_start_at_ms + buffer) is reached. Returns false when no start time
        /// is known yet so callers keep the board frozen and refresh room state to obtain it (callers
        /// must always bound this with their own anti-freeze timeout).
        /// </summary>
        public static bool IsGameplayStartTimeReached()
        {
            if (scheduledStartMs <= 0)
                return false;
            return ServerNowMs >= scheduledStartMs + GameplayStartOffsetMs;
        }

        public static double SecondsUntilGameplayStart()
        {
            if (scheduledStartMs <= 0)
                return 0d;
            return Math.Max(0d, (scheduledStartMs + GameplayStartOffsetMs - ServerNowMs) / 1000d);
        }

        public static int DisplayCountdownSeconds()
        {
            return Mathf.Max(0, Mathf.CeilToInt((float)SecondsUntilStart()));
        }

        public static void StartRoomClock()
        {
            if (scheduledStartMs > 0)
            {
                running = true;
                epochRealtime = Time.realtimeSinceStartupAsDouble;
                return;
            }

            epochRealtime = Time.realtimeSinceStartupAsDouble;
            running = true;
        }

        /// <summary>Milliseconds since synchronized room start (server time).</summary>
        public static double NowMs
        {
            get
            {
                if (!running) return 0d;

                if (scheduledStartMs > 0)
                    return Math.Max(0d, ServerNowMs - scheduledStartMs);

                return (Time.realtimeSinceStartupAsDouble - epochRealtime) * 1000d;
            }
        }

        public static void Reset()
        {
            running = false;
            epochRealtime = 0d;
            scheduledStartMs = 0;
            clockOffsetMs = 0;
        }

        private static long ServerNowMs =>
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + clockOffsetMs;
    }
}
