using System.Threading.Tasks;

namespace Mkey.Network
{
    public static class ProfileService
    {
        public static UserProfileDto CachedProfile { get; private set; }

        public static async Task<ApiResult<UserProfileDto>> RefreshProfileAsync()
        {
            if (ApiConfig.Current.UseLocalSimulation)
                return ApiResult<UserProfileDto>.Ok(null);

            var result = await NetworkManager.Instance.GetAsync<UserProfileDto>("auth/me");
            if (result.Success)
                CachedProfile = result.Data;

            return result;
        }
    }
}
