using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Базовый класс для всех ответов.
    /// </summary>
    public class ResponseBase
    {
        private const string _success = "Успешно";

        /// <summary>
        /// Возвращает <see cref="ResponseBase"/> с кодом -1
        /// </summary>
        /// <param name="message">сообщение, которое будет помещено в <see cref="Message"/></param>
        /// <returns></returns>
        public static ResponseBase Failure(string message) => new ResponseBase() { Message = message, Status = -1 };
        /// <summary>
        /// Возвращает <see cref="ResponseBase"/> с кодом 0
        /// </summary>
        /// <param name="message">сообщение, которое будет помещено в <see cref="Message"/></param>
        /// <returns></returns>
        public static ResponseBase Success(string message = _success) => new ResponseBase() { Message = message, Status = 0 };
        /// <summary>
        /// Создает экземпляр ответа с <see cref="Message"/> = <see cref="string.Empty"/> и <see cref="Status"/> = <see cref="0"/>
        /// </summary>
        public ResponseBase()
        {
            Message = string.Empty;
            Status = 0;
        }
        /// <summary>
        /// Статус операции
        /// </summary>
        [JsonPropertyName("status")]
        public int Status { get; set; } = 0;

        /// <summary>
        /// Сообщение об результате операции. На русском, можно отображать на frontend
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = _success;
    }
}
