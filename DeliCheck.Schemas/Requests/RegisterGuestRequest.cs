using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    /// <summary>
    /// Запрос для регистрации гостя (без ввода юзернейма и пароля). Кнопка "Продолжить без регистрации"
    /// </summary>
    public class RegisterGuestRequest
    {
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
