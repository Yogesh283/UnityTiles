using System.Threading.Tasks;
using Mkey;

namespace Mkey.Network
{
    public static class WalletService
    {
        public static int CachedBalance { get; internal set; }

        public static async Task<ApiResult<int>> FetchBalanceAsync()
        {
            if (ApiConfig.Current.UseLocalSimulation)
                return ApiResult<int>.Ok(CoinsHolder.Count);

            var result = await NetworkManager.Instance.GetAsync<WalletBalanceDto>("wallet/balance");
            if (!result.Success || result.Data == null)
                return ApiResult<int>.Fail(result.ErrorMessage, result.StatusCode, result.IsServerUnavailable);

            CachedBalance = result.Data.balance;
            return ApiResult<int>.Ok(CachedBalance);
        }

        public static async Task<ApiResult<int>> SyncToCoinsHolderAsync()
        {
            var result = await FetchBalanceAsync();
            if (result.Success && CoinsHolder.Instance)
                CoinsHolder.Instance.SetCount(result.Data);

            return result;
        }

        public static async Task<ApiResult<int>> CreditTournamentLevelRewardAsync(string roomId, int coins)
        {
            if (ApiConfig.Current.UseLocalSimulation)
            {
                if (CoinsHolder.Instance)
                    CoinsHolder.Add(coins);
                return ApiResult<int>.Ok(CoinsHolder.Instance ? CoinsHolder.Count : 0);
            }

            var body = new TournamentLevelRewardRequestDto { roomId = roomId };
            var result = await NetworkManager.Instance.PostAsync<TournamentLevelRewardRequestDto, WalletBalanceDto>(
                "tournaments/level-reward", body);

            if (!result.Success || result.Data == null)
                return ApiResult<int>.Fail(result.ErrorMessage, result.StatusCode, result.IsServerUnavailable);

            CachedBalance = result.Data.balance;
            return ApiResult<int>.Ok(CachedBalance);
        }
    }
}
