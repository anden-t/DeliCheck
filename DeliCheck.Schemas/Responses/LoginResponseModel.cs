using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Модель авторизации
    /// </summary>
    public class LoginResponseModel
    {
        /// <summary>
        /// ID пользователя 
        /// </summary>
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }
        /// <summary>
        /// Сессионный токен. Добавляется во все запросы в заголовок x-session-token
        /// </summary>
        [JsonPropertyName("session_token")]
        public string? SessionToken { get; set; }
        /// <summary>
        /// Время жизни токена в часах.
        /// </summary>
        [JsonPropertyName("token_expires_in")]
        public int ExpiresIn { get; set; }
    }
}
