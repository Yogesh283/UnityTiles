using System.Threading.Tasks;
using Mkey;

namespace Mkey.Network
{
    public static class PaymentService
    {
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
