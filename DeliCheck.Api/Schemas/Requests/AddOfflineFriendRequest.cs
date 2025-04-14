using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    /// <summary>
    /// Запрос на добавление незарегистированного пользователя
    /// </summary>
    public class AddOfflineFriendRequest 
    {
        /// <summary>
        /// Имя
        /// </summary>
        [Required]
        [JsonPropertyName("firstname")]
        public string Firstname { get; set; }
        /// <summary>
        /// Фамилия
        /// </summary>
        [Required]
        [JsonPropertyName("lastname")]
        public string Lastname { get; set; }
    }
}
