using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.SignalR.Requests
{
    /// <summary>
    /// Запрос SignalR на клик по позиции
    /// </summary>
    public class SelectItemRequest : BaseSplittingRequest
    {
        /// <summary>
        /// Имя метода
        /// </summary>
        public const string MethodName = "SelectItem";

        /// <summary>
        /// Идентификатор позиции
        /// </summary>
        [JsonPropertyName("item_id")]
        public int ItemId { get; set; }
    }
}
