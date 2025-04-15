using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    /// <summary>
    /// Запрос на изменение пароля
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// Старый пароль
        /// </summary>
        [Required]
        [JsonPropertyName("old_password")]
        public string OldPassword { get; set; }
        /// <summary>
        /// Новый пароль. От 6 до 50 символов.
        /// </summary>
        [Required]
        [JsonPropertyName("new_password")]
        public string NewPassword { get; set; }
    }
}
