using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.SignalR.Responses
{
    /// <summary>
    /// Сообщение SignalR об ошибке
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Имя метода
        /// </summary>
        public const string MethodName = "Error";
        /// <summary>
        /// Уровень ошибки
        /// </summary>
        [JsonPropertyName("level")]
        public ErrorLevel Level { get; set; }
        /// <summary>
        /// Текст сообщения
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    /// <summary>
    /// Уровень ошибки
    /// </summary>
    public enum ErrorLevel
    {
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
