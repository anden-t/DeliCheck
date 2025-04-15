using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Ответ на авторизацию через ВК
    /// </summary>
    public class VkAuthResponseModel
    {
        /// <summary>
        /// URL OAuth авторизации ВК. Приложение должно перебросить пользователя на данный URL.
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
