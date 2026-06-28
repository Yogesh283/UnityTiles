using System.Threading.Tasks;
using Mkey;
using UnityEngine;

namespace Mkey.Network
{
    public static class AuthService
    {
        private const string GuestIdKey = "mk_guest_id";

        public static string GetOrCreateGuestId()
        {
            if (PlayerPrefs.HasKey(GuestIdKey))
                return PlayerPrefs.GetString(GuestIdKey);

            string guestId = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(guestId))
                guestId = "guest_" + System.Guid.NewGuid().ToString("N");

            PlayerPrefs.SetString(GuestIdKey, guestId);
            PlayerPrefs.Save();
            return guestId;
        }

        public static async Task<ApiResult<TokenResponseDto>> RegisterAsync(string email, string password, string displayName)
        {
            var body = new RegisterRequestDto
            {
                email = email,
                password = password,
                displayName = displayName
            };
            return await CompleteLoginAsync(
                await NetworkManager.Instance.PostAsync<RegisterRequestDto, TokenResponseDto>("auth/register", body, false));
        }

        public static async Task<ApiResult<TokenResponseDto>> LoginAsync(string email, string password)
        {
            var body = new LoginRequestDto { email = email, password = password };
            return await CompleteLoginAsync(
                await NetworkManager.Instance.PostAsync<LoginRequestDto, TokenResponseDto>("auth/login", body, false));
        }

        public static async Task<ApiResult<TokenResponseDto>> GuestLoginAsync(string displayName = null)
        {
            string guestId = GetOrCreateGuestId();
            if (string.IsNullOrEmpty(displayName))
                displayName = PlayerDataHolder.Instance ? PlayerDataHolder.FullName : "Guest";

            var body = new GuestLoginRequestDto
            {
                guestId = guestId,
                displayName = displayName
            };

            return await CompleteLoginAsync(
                await NetworkManager.Instance.PostAsync<GuestLoginRequestDto, TokenResponseDto>("auth/guest", body, false));
        }

        private static async Task<ApiResult<TokenResponseDto>> CompleteLoginAsync(ApiResult<TokenResponseDto> result)
        {
            if (!result.Success || result.Data == null)
                return result;

            NetworkManager.Instance.SaveSession(result.Data.accessToken, result.Data.userId);

            if (PlayerDataHolder.Instance && !string.IsNullOrEmpty(result.Data.displayName))
                PlayerDataHolder.Instance.SetFullName(result.Data.displayName);

            await ProfileService.RefreshProfileAsync();
            return result;
        }

        public static void Logout()
        {
            NetworkManager.Instance.ClearSession();
        }
    }
}
