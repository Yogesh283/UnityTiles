using UnityEngine;

namespace Mkey
{
    /// <summary>
    /// Campaign level play clock: 3-minute countdown.
    /// Victory TIME uses elapsed (time used); HUD shows remaining.
    /// Supports Pause/Resume for the React Native shell without changing match rules.
    /// </summary>
    public static class CampaignLevelTimer
    {
        public const float DurationSeconds = 180f; // 3 minutes

        private static float startedAtRealtime = -1f;
        private static float stoppedElapsed = -1f;
        private static float pausedAtRealtime = -1f;
        private static float pausedAccumulated;
        private static bool running;

        public static bool IsRunning => running && pausedAtRealtime < 0f;
        public static bool IsPaused => running && pausedAtRealtime >= 0f;

        public static void Start()
        {
            startedAtRealtime = Time.realtimeSinceStartup;
            stoppedElapsed = -1f;
            pausedAtRealtime = -1f;
            pausedAccumulated = 0f;
            running = true;
        }

        public static void Stop()
        {
            if (!running) return;
            stoppedElapsed = Mathf.Clamp(ComputeElapsed(), 0f, DurationSeconds);
            running = false;
            pausedAtRealtime = -1f;
        }

        public static void Reset()
        {
            running = false;
            startedAtRealtime = -1f;
            stoppedElapsed = -1f;
            pausedAtRealtime = -1f;
            pausedAccumulated = 0f;
        }

        /// <summary>Freezes the campaign clock (RN Pause). Does not change DurationSeconds.</summary>
        public static void Pause()
        {
            if (!running || pausedAtRealtime >= 0f) return;
            pausedAtRealtime = Time.realtimeSinceStartup;
        }

        /// <summary>Resumes after <see cref="Pause"/>.</summary>
        public static void Resume()
        {
            if (!running || pausedAtRealtime < 0f) return;
            pausedAccumulated += Time.realtimeSinceStartup - pausedAtRealtime;
            pausedAtRealtime = -1f;
        }

        public static float ElapsedSeconds
        {
            get
            {
                if (stoppedElapsed >= 0f) return stoppedElapsed;
                if (!running || startedAtRealtime < 0f) return 0f;
                return Mathf.Clamp(ComputeElapsed(), 0f, DurationSeconds);
            }
        }

        public static float RemainingSeconds => Mathf.Max(0f, DurationSeconds - ElapsedSeconds);

        public static bool IsExpired => RemainingSeconds <= 0f && (running || stoppedElapsed >= 0f);

        public static string FormatElapsed() => Format(ElapsedSeconds);

        public static string FormatRemaining() => Format(RemainingSeconds);

        private static float ComputeElapsed()
        {
            float now = Time.realtimeSinceStartup;
            float pausedExtra = pausedAccumulated;
            if (pausedAtRealtime >= 0f)
                pausedExtra += now - pausedAtRealtime;
            return (now - startedAtRealtime) - pausedExtra;
        }

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
