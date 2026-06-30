using System;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Room-authoritative clock synced to backend match_start_at_ms (same for all players).
    /// </summary>
    public static class TournamentServerClock
    {
        private static double epochRealtime;
        private static bool running;
        private static long scheduledStartMs;
        private static long clockOffsetMs;

        public static bool IsRunning => running;

        public static long ScheduledStartMs => scheduledStartMs;

        /// <summary>Align local clock to server time from the latest room payload.</summary>
        public static void SyncServerTime(long serverNowMs)
        {
            if (serverNowMs <= 0) return;
            clockOffsetMs = serverNowMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static void ScheduleServerStart(long matchStartAtMs)
        {
            if (matchStartAtMs <= 0) return;
            scheduledStartMs = matchStartAtMs;
        }

        public static bool IsStartTimeReached()
        {
            if (scheduledStartMs <= 0)
                return true;
            return ServerNowMs >= scheduledStartMs;
        }

        public static double SecondsUntilStart()
        {
            if (scheduledStartMs <= 0)
                return 0d;
            return Math.Max(0d, (scheduledStartMs - ServerNowMs) / 1000d);
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
