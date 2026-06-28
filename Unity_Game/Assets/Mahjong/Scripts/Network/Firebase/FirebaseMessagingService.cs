using UnityEngine;

namespace Mkey.Network.Firebase
{
    /// <summary>
    /// Firebase Cloud Messaging stub. Register token via POST /api/v1/notifications/fcm/register.
    /// </summary>
    public static class FirebaseMessagingService
    {
        public static void RegisterTokenWithBackend()
        {
            if (ApiConfig.Current.UseLocalSimulation)
                return;

#if FIREBASE
            // string token = await FirebaseMessaging.GetTokenAsync();
            // await NetworkManager.Instance.PostAsync<FcmRegisterDto, OkResponseDto>(...);
#endif
            Debug.Log("[FCM] Token registration stub — enable FIREBASE + Firebase SDK");
        }

        public static void OnTournamentStarted(string tournamentName)
        {
            Debug.Log("[FCM] Local notification stub: Tournament started — " + tournamentName);
        }
    }
}
