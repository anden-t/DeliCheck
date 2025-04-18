using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Модель профиля пользователя
    /// </summary>
    public class ProfileResponseModel
    {
        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }
        /// <summary>
        /// Имя (отображаемое)
        /// </summary>
        [JsonPropertyName("firstname")]
        public string Firstname { get; set; }
        /// <summary>
        /// Фамилия
        /// </summary>
        [JsonPropertyName("lastname")]
        public string Lastname { get; set; }
        /// <summary>
        /// Юзернейм
        /// </summary>
        [JsonPropertyName("username")]
        public string Username { get; set; }
        /// <summary>
        /// Есть ли аватар
        /// </summary>
        [JsonPropertyName("has_avatar")]
        public bool HasAvatar { get; set; }
        /// <summary>
        /// Показывает, свой ли это профиль. Если свой true, иначе false
        /// </summary>
        [JsonPropertyName("self")]
        public bool Self { get; set; }
        /// <summary>
        /// Эл. почта.
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        /// <summary>
        /// Номер телефона.
        /// </summary>
        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }
        /// <summary>
        /// Идентификатор пользователя в ВК. null, если профиль не привязан
        /// </summary>
        [JsonPropertyName("vk_id")]
        public long? VkId { get; set; }

        public string AvatarUrl => $"https://api.deli-check.ru/avatars/user?userId={Id}";
    }
}
