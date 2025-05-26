using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Информация о позиции чека
    /// </summary>
    public class InvoiceItemResponseModel
    {
        /// <summary>
        /// Идентификатор 
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }
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
        /// Стоимость всей позиции (всего количества)
        /// </summary>
        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }
        /// <summary>
        /// Тип количества (отображаемый)
        /// </summary>
        [JsonPropertyName("quantity_measure")]
        public ItemQuantityMeasure QuantityMeasure { get; set; }
    }
}
