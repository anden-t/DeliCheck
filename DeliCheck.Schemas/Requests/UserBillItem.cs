using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    /// <summary>
    /// Модель для позиции счета
    /// </summary>
    public class UserBillItem
    {
        /// <summary>
        /// Илентификатор позиции в чеке
        /// </summary>
        [Required]
        [JsonPropertyName("item_id")]
        public int ItemId { get; set; }
        /// <summary>
        /// Количество в позиции, которое принаджежит пользователю
        /// </summary>
        [Required]
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
    }
}
