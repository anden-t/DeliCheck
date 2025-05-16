namespace DeliCheck.Services
{
    /// <summary>
    /// Интерфейс сервиса, предназначенного для работы с аватарами пользователей
    /// </summary>
    public interface IAvatarService
    {
        /// <summary>
        /// Получить путь к аватару пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns></returns>
        string GetPathForUserAvatar(int userId);
        /// <summary>
        /// Сохраняет аватар пользователя, конвертируя его в формат JPG с разрешением 200x200. Возвращает true, если удалось сохранить, иначе false
        /// </summary>
        /// <param name="stream">Поток с изображением</param>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns></returns>
        Task<bool> SaveUserAvatarAsync(Stream stream, int userId, bool skipCrop = false);
        /// <summary>
        /// Сохраняет аватар незарегистрированного друга, конвертируя его в формат JPG с разрешением 200x200. Возвращает true, если удалось сохранить, иначе false
        /// </summary>
        /// <param name="stream">Поток с изображением</param>
        /// <param name="friendId">Идентификатор друга</param>
        /// <returns></returns>
        Task<bool> SaveFriendAvatarAsync(Stream stream, int friendId, bool skipCrop = false);
        /// <summary>
        /// Получает аватар пользователя. Возвращает null, если аватара не существует
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns></returns>
        FileStream? GetUserAvatar(int userId);
        /// <summary>
        /// Получает аватар незарегистрированного друга. Возвращает null, если аватара не существует
        /// </summary>
        /// <param name="friendId">Идентификатор друга</param>
        /// <returns></returns>
        FileStream? GetFriendAvatar(int friendId);
        /// <summary>
        /// Получает аватар по умолчанию для пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <returns></returns>
        FileStream GetDefaultAvatar();
        /// <summary>
        /// Удалить аватар у пользователя 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        bool RemoveUserAvatar(int userId);
        /// <summary>
        /// Удалить аватар у друга 
        /// </summary>
        /// <param name="friendId"></param>
        /// <returns></returns>
        bool RemoveFriendAvatar(int friendId);

    }
}