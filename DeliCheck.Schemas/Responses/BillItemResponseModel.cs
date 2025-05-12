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
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
        /// <summary>
        /// Мера количества (отображаемая)
        /// </summary>
        [JsonPropertyName("quantity_measure")]
        public ItemQuantityMeasure QuantityMeasure { get; set; }
    }
}
