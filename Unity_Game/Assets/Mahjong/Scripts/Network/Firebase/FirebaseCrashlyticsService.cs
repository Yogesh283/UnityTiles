using UnityEngine;

namespace Mkey.Network.Firebase
{
    /// <summary>
    /// Firebase Crashlytics stub. Add Firebase Unity SDK and define FIREBASE to enable.
    /// </summary>
    public static class FirebaseCrashlyticsService
    {
        public static void Initialize()
        {
#if FIREBASE
            // FirebaseCrashlytics.SetCrashlyticsCollectionEnabled(true);
#endif
            Application.logMessageReceived += OnLogMessage;
            Debug.Log("[Crashlytics] Initialized (stub)");
        }

        public static void LogException(System.Exception ex)
        {
#if FIREBASE
            // FirebaseCrashlytics.LogException(ex);
#endif
            Debug.LogWarning("[Crashlytics] " + ex.Message);
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error)
            {
#if FIREBASE
                // FirebaseCrashlytics.Log(condition + "\n" + stackTrace);
#endif
            }
        }
    }
}
