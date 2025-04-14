using DeliCheck.Models;

namespace DeliCheck.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Задает время жизни токена по умолчанию
        /// </summary>
        int DefaultExpiresIn { get; }
        /// <summary>
        /// Создает новый токен для пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns></returns>
        Task<SessionTokenModel> CreateSessionTokenAsync(int userId, string device);
        /// <summary>
        /// Обновляет время жизни старого токена. Возвращает false, если не удалось обновить.
        /// </summary>
        /// <param name="oldToken">Старый токен</param>
        /// <returns>false, если не удалось обновить, иначе true</returns>
        Task<bool> RefreshSessionTokenAsync(SessionTokenModel oldToken);
        /// <summary>
        /// Возвращает модель токена по строке. Возвращает null, если токен неактивный либо не найден.
        /// </summary>
        /// <param name="token">Токен</param>
        /// <returns></returns>
        SessionTokenModel? GetSessionTokenByString(string token);
        /// <summary>
        /// Возвращает модель пользователя по токену. Возвращает null, если пользователь не найден или токен неактивный 
        /// </summary>
        /// <param name="token">Токен</param>
        /// <returns></returns>
        UserModel? GetUserByToken(SessionTokenModel token);
        /// <summary>
        /// Возвращает модель пользователя по строке токену. Возвращает null, если пользователь не найден или токен неактивный 
        /// </summary>
        /// <param name="token">Токен в виде строки</param>
        /// <returns></returns>
        UserModel? GetUserByStringToken(string token);
        /// <summary>
        /// Делает неактивным токен сессии
        /// </summary>
        /// <param name="token">Токен</param>
        Task DisableTokenAsync(SessionTokenModel token);
    }
}
