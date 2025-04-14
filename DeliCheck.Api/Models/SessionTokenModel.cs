namespace DeliCheck.Models
{
    /// <summary>
    /// Модель токена сессии
    /// </summary>
    public class SessionTokenModel
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Токен в формате USER_ID.GUID
        /// </summary>
        public string SessionToken { get; set; }
        /// <summary>
        /// Время создания токена (UTC)
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// Устройство, с которого был создан токен
        /// </summary>
        public string Device { get; set; }
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        public int UserId => int.Parse(SessionToken.Split(".")[0]);
        /// <summary>
        /// Отключен ли токен (например, при выходе из аккаунта). Значенние по умолчанию: <see cref="false"/>
        /// </summary>
        public bool Disabled { get; set; }
        /// <summary>
        /// Количество часов, через которое токен станет неактивным
        /// </summary>
        public int ExpiresInHours { get; set; }
        /// <summary>
        /// Активен ли токен
        /// </summary>
        public bool IsActive => !Disabled && DateTime.UtcNow < CreateTime + new TimeSpan(ExpiresInHours, 0, 0);
    }
}
