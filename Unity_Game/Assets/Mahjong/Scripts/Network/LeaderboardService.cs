using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mkey.Network
{
    public static class LeaderboardService
    {
        public static List<LeaderboardEntryDto> CachedEntries { get; private set; } = new List<LeaderboardEntryDto>();

        public static async Task<ApiResult<List<LeaderboardEntryDto>>> FetchLeaderboardAsync(int limit = 50)
        {
            if (ApiConfig.Current.UseLocalSimulation)
                return ApiResult<List<LeaderboardEntryDto>>.Ok(new List<LeaderboardEntryDto>());

            var result = await NetworkManager.Instance.GetAsync<List<LeaderboardEntryDto>>(
                "leaderboard?limit=" + limit, requireAuth: false);

            if (result.Success && result.Data != null)
                CachedEntries = result.Data;

            return result;
        }
    }
}
