using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    /// <summary>
    /// Модель для счета для конкретного пользователя
    /// </summary>
    public class UserBill
    {
        /// <summary>
        /// Незарегистирован ли пользователь, которому принадлежит счет
        /// </summary>
        [Required]
        [JsonPropertyName("offline_owner")]
        public bool OfflineOwner { get; set; }
        /// <summary>
        /// Идентификатор пользователя, которому будет выставлен счет
        /// </summary>
        [Required]
        [JsonPropertyName("owner_id")]
        public int OwnerId { get; set; }
        /// <summary>
        /// Позиции пользователя
        /// </summary>
        [Required]
        [JsonPropertyName("items")]
        public List<UserBillItem> Items { get; set; }
    }
}
