using System.Threading.Tasks;
using Mkey.Network;

namespace Mkey
{
    /// <summary>
    /// Requests backend level-complete rewards (50 coins every time a level is won, levels 1-300).
    /// </summary>
    public static class LevelCompletionRewardService
    {
        public const int CoinsPerLevel = 50;
        public const int MaxLevel = 300;

        public static int ToLevelNumber(int levelIndex) => levelIndex + 1;

        public static bool IsEligibleLevel(int levelNumber) =>
            levelNumber >= 1 && levelNumber <= MaxLevel;

        public static async Task<ApiResult<LevelCompleteResponseDto>> TryGrantRewardAsync(int levelIndex)
        {
            int levelNumber = ToLevelNumber(levelIndex);
            if (!IsEligibleLevel(levelNumber))
                return ApiResult<LevelCompleteResponseDto>.Fail("Invalid level number.");

            if (ApiConfig.Current.UseLocalSimulation)
                return ApiResult<LevelCompleteResponseDto>.Fail("Development mode enabled.");

            if (!await AuthService.EnsureSessionAsync())
                return ApiResult<LevelCompleteResponseDto>.Fail("Not authenticated.");

            string userUuid = NetworkManager.Instance.UserUuid;

            var body = new LevelCompleteRequestDto
            {
                userUuid = userUuid,
                levelNumber = levelNumber
            };
            return await NetworkManager.Instance.PostAsync<LevelCompleteRequestDto, LevelCompleteResponseDto>(
                "levels/complete", body);
        }
    }
}
