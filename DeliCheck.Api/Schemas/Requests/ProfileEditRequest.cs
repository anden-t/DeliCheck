using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    /// <summary>
    /// Запрос на изменение информации пользователя
    /// </summary>
    public class ProfileEditRequest
    {
        /// <summary>
        /// Имя (отображаемое)
        /// </summary>
        [JsonPropertyName("firstname")]
        public string? Firstname { get; set; }
        /// <summary>
        /// Фамилия
        /// </summary>
        [JsonPropertyName("lastname")]
        public string? Lastname { get; set; }
        /// <summary>
        /// Эл. почта
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        /// <summary>
        /// Номер телефона
        /// </summary>
        [JsonPropertyName("phone")]
        public string? PhoneNumber { get; set; }
        /// <summary>
        /// Имя пользователя
        /// </summary>
        [JsonPropertyName("username")]
        public string? Username { get; set; }
    }
}
