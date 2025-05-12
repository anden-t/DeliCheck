using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Информация о счете
    /// </summary>
    public class BillResponseModel
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }
        /// <summary>
        /// Незаристривован ли пользователь, которому выставлен счет (true - незарегестрирован, false - зарегистрирован)
        /// </summary>
        [JsonPropertyName("offline_owner")]
        public bool OfflineOwner { get; set; }
        /// <summary>
        /// Идентификатор пользователя, которому выставлен счет. Если offline_owner = true, то это friendLabelId, если false - userId
        /// </summary>
        [JsonPropertyName("owner_id")]
        public int OwnerId { get; set; }
        /// <summary>
        /// Идентификатор чека, по которому выставлен счет
        /// </summary>
        [JsonPropertyName("invoice_id")]
        public int InvoiceId { get; set; }
        /// <summary>
        /// Список позиций для оплаты
        /// </summary>
        [JsonPropertyName("items")]
        public List<BillItemResponseModel> Items { get; set; }
        /// <summary>
        /// Сумма
        /// </summary>
        [JsonPropertyName("total_cost")]
        public decimal TotalCost { get; set; }
        /// <summary>
        /// Оплачен ли счет
        /// </summary>
        [JsonPropertyName("payed")]
        public bool Payed { get; set; }
        /// <summary>
        /// Пользователь, которому принадлежит счет
        /// </summary>
        [JsonPropertyName("owner")]
        public FriendResponseModel Owner { get; set; }
        /// <summary>
        /// Пользователь, которому принадлежит чек
        /// </summary>
        [JsonPropertyName("invoice_owner_firstname")]
        public FriendResponseModel InvoiceOwner { get; set; }
    }
}
