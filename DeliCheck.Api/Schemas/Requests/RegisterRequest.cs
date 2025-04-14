using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    /// <summary>
    /// Запрос для регистрации пользователя
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// Имя пользователя, от 4 до 20 символов.
        /// </summary>
        [Required]
        [JsonPropertyName("username")]
        public string Username { get; set; }
        /// <summary>
        /// Эл. почта
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        /// <summary>
        /// Пароль, от 6 до 50 символов
        /// </summary>
        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
        /// <summary>
        /// Номер телефона. Формат +79998881122
        /// </summary>
        [JsonPropertyName("phone")]
        public string? PhoneNumber { get; set; }
        /// <summary>
        /// Имя (отображаемое)
        /// </summary>
        [Required]
        [JsonPropertyName("firstname")]
        public string Firstname { get; set; }
        /// <summary>
        /// Фамилия
        /// </summary>
        [JsonPropertyName("lastname")]
        public string? Lastname { get; set; }
    }
}
