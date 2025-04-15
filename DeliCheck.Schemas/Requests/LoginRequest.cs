using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    /// <summary>
    /// Запрос на авторизацию
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Юзернейм
        /// </summary>
        [Required]
        [JsonPropertyName("username")]
        public string Username { get; set; }
        /// <summary>
        /// Пароль
        /// </summary>
        [Required]
        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}
