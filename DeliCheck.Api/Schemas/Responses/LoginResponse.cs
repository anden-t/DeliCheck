using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Ответ для входа в аккаунт (через логин или регистрацию)
    /// </summary>
    public class LoginResponse : ResponseBase
    {
        public LoginResponse() : base() { }
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
