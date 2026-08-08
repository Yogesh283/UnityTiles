using UnityEngine;

namespace Mkey
{
    /// <summary>
    /// Campaign level play clock: 5-minute countdown.
    /// Victory TIME uses elapsed (time used); HUD shows remaining.
    /// </summary>
    public static class CampaignLevelTimer
    {
        public const float DurationSeconds = 300f; // 5 minutes

        private static float startedAtRealtime = -1f;
        private static float stoppedElapsed = -1f;
        private static bool running;

        public static bool IsRunning => running;

        public static void Start()
        {
            startedAtRealtime = Time.realtimeSinceStartup;
            stoppedElapsed = -1f;
            running = true;
        }

        public static void Stop()
        {
            if (!running) return;
            stoppedElapsed = Mathf.Clamp(Time.realtimeSinceStartup - startedAtRealtime, 0f, DurationSeconds);
            running = false;
        }

        public static void Reset()
        {
            running = false;
            startedAtRealtime = -1f;
            stoppedElapsed = -1f;
        }

        public static float ElapsedSeconds
        {
            get
            {
                if (stoppedElapsed >= 0f) return stoppedElapsed;
                if (!running || startedAtRealtime < 0f) return 0f;
                return Mathf.Clamp(Time.realtimeSinceStartup - startedAtRealtime, 0f, DurationSeconds);
            }
        }

        public static float RemainingSeconds => Mathf.Max(0f, DurationSeconds - ElapsedSeconds);

        public static bool IsExpired => RemainingSeconds <= 0f && (running || stoppedElapsed >= 0f);

        public static string FormatElapsed() => Format(ElapsedSeconds);

        public static string FormatRemaining() => Format(RemainingSeconds);

        private static string Format(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            if (total > Mathf.CeilToInt(DurationSeconds)) total = Mathf.CeilToInt(DurationSeconds);
            int minutes = total / 60;
            int secs = total % 60;
            return $"{minutes:00}:{secs:00}";
        }
    }
}
