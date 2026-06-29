using System.Threading.Tasks;
using Mkey;

namespace Mkey.Network
{
    public static class PaymentService
    {
        public static bool BillingActive { get; private set; }
        public static string BillingStatusMessage { get; private set; }

        public static async Task<ApiResult<GooglePlayBillingStatusDto>> FetchBillingStatusAsync()
        {
            if (ApiConfig.Current.UseLocalSimulation)
            {
                BillingActive = false;
                BillingStatusMessage = "Development mode enabled.";
                return ApiResult<GooglePlayBillingStatusDto>.Fail(BillingStatusMessage);
            }

            var result = await NetworkManager.Instance.GetAsync<GooglePlayBillingStatusDto>(
                "payments/google/status",
                false);

            if (result.Success && result.Data != null)
            {
                BillingActive = result.Data.active || result.Data.configured;
                BillingStatusMessage = BillingActive
                    ? "Google Play billing is active."
                    : (string.IsNullOrEmpty(result.Data.error)
                        ? "Google Play billing is not configured on the server."
                        : result.Data.error);
            }
            else
            {
                BillingActive = false;
                BillingStatusMessage = result.ErrorMessage ?? "Could not check billing status.";
            }

            return result;
        }

        public static async Task<ApiResult<GooglePlayVerifyResponseDto>> VerifyGooglePlayAsync(
            string productId,
            string purchaseToken)
        {
            if (ApiConfig.Current.UseLocalSimulation)
                return ApiResult<GooglePlayVerifyResponseDto>.Fail("Development mode enabled.");

            var body = new GooglePlayVerifyRequestDto
            {
                productId = productId,
                purchaseToken = purchaseToken,
            };

            var result = await NetworkManager.Instance.PostAsync<
                GooglePlayVerifyRequestDto,
                GooglePlayVerifyResponseDto>("payments/google/verify", body);

            if (result.Success && result.Data != null)
                WalletService.CachedBalance = result.Data.balance;

            return result;
        }

        public static async Task<ApiResult<IapProductDto[]>> FetchProductsAsync()
        {
            if (ApiConfig.Current.UseLocalSimulation)
                return ApiResult<IapProductDto[]>.Fail("Development mode enabled.");

            return await NetworkManager.Instance.GetAsync<IapProductDto[]>("payments/products", false);
        }
    }
}
