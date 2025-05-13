using DeliCheck.Models;

namespace DeliCheck.Services
{
    public interface IVkApi
    {
        Task<VkAuthorizationData> Login(string authorizationCode, string deviceId, string codeVerifier, string state);
        Task<VkAuthorizationData> RefreshTokenAsync(VkAuthorizationData authData);
        Task UpdateUserInfoAsync(VkAuthorizationData authData, bool setAvatar = true);
        Task UpdateFriendsListAsync(VkAuthorizationData authData);

        string GetVkAuthUrl(string codeChallenge, string state);
        string GetVkAuthUrl(string codeChallenge, string state, string returnUrl);
        string GetConnectVkAuthUrl(string codeChallenge, string state);
        string GetConnectVkAuthUrl(string codeChallenge, string state, string returnUrl);
        string GetState();
        string GetCodeVerifier();
        string GetCodeChallenge(string codeVerifier);
    }
}
