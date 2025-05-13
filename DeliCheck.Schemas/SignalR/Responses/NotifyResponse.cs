using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.SignalR.Responses
{
    /// <summary>
    /// Сообщение SignalR об ошибке
    /// </summary>
    public class NotifyResponse
    {
        /// <summary>
        /// Имя метода
        /// </summary>
        public const string MethodName = "Notify";
        /// <summary>
        /// Уровень ошибки
        /// </summary>
        [JsonPropertyName("level")]
        public NotifyLevel Level { get; set; }
        /// <summary>
        /// Текст сообщения
        /// </summary>
        [JsonPropertyName("detail")]
        public string Detail { get; set; }
        [JsonPropertyName("summary")]
        public string Summary { get; set; }
    }

    /// <summary>
    /// Уровень ошибки
    /// </summary>
    public enum NotifyLevel
    {
        /// <summary>
        /// Успех
        /// </summary>
        Success,
        /// <summary>
        /// Информация
        /// </summary>
        Info,
        /// <summary>
        /// Предупреждение
        /// </summary>
        Warning,
        /// <summary>
        /// Ошибка
        /// </summary>
        Error
    }
}
