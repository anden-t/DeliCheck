using DeliCheck.Models;

namespace DeliCheck.Services
{
    /// <summary>
    /// Интерфейс для провайдера авторизации
    /// </summary>
    public interface IAuthProvider
    {
        Task<SessionTokenModel?> LoginByUsernameAsync(string username, string password, string device);
        Task<SessionTokenModel?> LoginByEmailAsync(string email, string password, string device);
        Task<SessionTokenModel?> LoginByPhoneNumberAsync(string phoneNumber, string password, string device);
        Task<RegisterResult> RegisterAsync(string username, string email, string phoneNumber, string password, string firstName, string lastName, long? vkId = null);
        Task<bool> ChangePasswordAsync(int userId, string newPassword, string oldPassword);
        Task<(VkAuthorizationData, SessionTokenModel, UserModel)> LoginVkAsync(string authorizationCode, string deviceId, string codeVerifier, string state, string device);
        Task<VkAuthorizationData?> ConnectVkAsync(string authorizationCode, string deviceId, string codeVerifier, string state, int userId);
        string GetGuestUsername();
        string GetRandomPassword();
    }
}
