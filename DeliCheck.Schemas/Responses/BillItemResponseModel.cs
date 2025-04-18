using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Позиция в счете
    /// </summary>
    public class BillItemResponseModel
    {
        /// <summary>
        /// Стоимость позиции (кол-во входит в стоимость, т. е. это цена за текущую Count)
        /// </summary>
        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }
        /// <summary>
        /// Название позиции
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// Количество в позиции
        /// </summary>
        [JsonPropertyName("count")]
        public decimal Count { get; set; }
    }
}
