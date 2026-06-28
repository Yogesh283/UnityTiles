using UnityEngine;

namespace Mkey.Network.Firebase
{
    /// <summary>
    /// Firebase bootstrap stub. Add Firebase Unity SDK and define FIREBASE to enable.
    /// Tracks: app opens, sessions, retention, tournaments, purchases.
    /// </summary>
    public static class FirebaseAnalyticsService
    {
        public static void LogAppOpen()
        {
            LogEvent("app_open");
        }

        public static void LogTournamentJoin(string tournamentId)
        {
            LogEvent("tournament_join", "tournament_id", tournamentId);
        }

        public static void LogTournamentResult(string tournamentId, int rank, int prize)
        {
#if FIREBASE
            // FirebaseAnalytics.LogEvent("tournament_result", ...);
#endif
            Debug.Log("[Analytics] tournament_result " + tournamentId + " rank=" + rank + " prize=" + prize);
        }

        public static void LogPurchase(string productId, int coins)
        {
            LogEvent("purchase", "product_id", productId);
            Debug.Log("[Analytics] purchase " + productId + " coins=" + coins);
        }

        private static void LogEvent(string name, string paramKey = null, string paramValue = null)
        {
#if FIREBASE
            // FirebaseAnalytics.LogEvent(name);
#endif
            Debug.Log("[Analytics] " + name);
        }
    }
}
