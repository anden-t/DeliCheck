namespace DeliCheck.Models
{
    /// <summary>
    /// Модель незарегистрированного в приложении друга
    /// </summary>
    public class OfflineFriendModel
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Идентификатор пользователя, кто добавил в друзья
        /// </summary>
        public int OwnerId { get; set; }
        /// <summary>
        /// Имя друга
        /// </summary>
        public string Firstname { get; set; }
        /// <summary>
        /// Фамилия друга
        /// </summary>
        public string Lastname { get; set; }
        /// <summary>
        /// Идентификатор пользователя в ВК
        /// </summary>
        public long? VkId { get; set; }

        /// <summary>
        /// Есть ли аватар у друга
        /// </summary>
        public bool HasAvatar { get; set; }
    }
}
