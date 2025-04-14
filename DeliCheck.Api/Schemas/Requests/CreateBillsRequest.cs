using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    /// <summary>
    /// Модель для создания счетов по чеку
    /// </summary>
    public class CreateBillsRequest
    {
        /// <summary>
        /// Илентификатор чека
        /// </summary>
        [Required]
        [JsonPropertyName("invoice_id")]
        public int InvoiceId { get; set; }
        /// <summary>
        /// Счета для пользователей
        /// </summary>
        [Required]
        [JsonPropertyName("bills")]
        public List<UserBill> Bills { get; set; }
    }
}
