using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Mkey.Network
{
    public static class NetworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            NetworkManager.EnsureExists();
            NetworkManager.Instance.StartCoroutine(BootstrapRoutine());
        }

        private static IEnumerator BootstrapRoutine()
        {
            if (ApiConfig.Current.UseLocalSimulation)
                yield break;

            if (!NetworkManager.Instance.IsAuthenticated)
            {
                Task<ApiResult<TokenResponseDto>> loginTask = AuthService.GuestLoginAsync();
                while (!loginTask.IsCompleted)
                    yield return null;

                if (!loginTask.Result.Success)
                {
                    Debug.LogWarning(
                        "[NetworkBootstrap] Auto login failed: " + loginTask.Result.ErrorMessage +
                        " (Start the FastAPI server, or enable Development Mode on Resources/Network/ApiConfig.)");
                }
                else
                {
                    Debug.Log("[NetworkBootstrap] Connected to backend at " + ApiConfig.Current.ServerRoot +
                              " (user " + loginTask.Result.Data.userUuid + ").");
                    Task<ApiResult<int>> walletTask = WalletService.SyncToCoinsHolderAsync();
                    while (!walletTask.IsCompleted)
                        yield return null;

                    Task billingTask = PaymentService.FetchBillingStatusAsync();
                    while (!billingTask.IsCompleted)
                        yield return null;

                    Debug.Log("[NetworkBootstrap] Billing: " +
                              (PaymentService.BillingActive ? "active" : PaymentService.BillingStatusMessage));
                }
            }
            else
            {
                if (string.IsNullOrEmpty(NetworkManager.Instance.UserUuid))
                {
                    Task<ApiResult<TokenResponseDto>> reloginTask = AuthService.GuestLoginAsync();
                    while (!reloginTask.IsCompleted)
                        yield return null;

                    if (!reloginTask.Result.Success)
                    {
                        Debug.LogWarning(
                            "[NetworkBootstrap] Re-login for user UUID failed: " +
                            reloginTask.Result.ErrorMessage);
                    }
                }

                Task<ApiResult<UserProfileDto>> profileTask = ProfileService.RefreshProfileAsync();
                while (!profileTask.IsCompleted)
                    yield return null;

                Task<ApiResult<int>> walletTask = WalletService.SyncToCoinsHolderAsync();
                while (!walletTask.IsCompleted)
                    yield return null;
            }
        }
    }
}
