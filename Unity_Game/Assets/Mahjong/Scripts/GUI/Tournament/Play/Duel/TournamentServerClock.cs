using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Room-authoritative monotonic clock (realtime since room sync — not wall/device DateTime).
    /// </summary>
    public static class TournamentServerClock
    {
        private static double epochRealtime;
        private static bool running;

        public static bool IsRunning => running;

        public static void StartRoomClock()
        {
            epochRealtime = Time.realtimeSinceStartupAsDouble;
            running = true;
        }

        /// <summary>Milliseconds since synchronized room start.</summary>
        public static double NowMs
        {
            get
            {
                if (!running) return 0d;
                return (Time.realtimeSinceStartupAsDouble - epochRealtime) * 1000d;
            }
        }

        public static void Reset()
        {
            running = false;
            epochRealtime = 0d;
        }
    }
}
