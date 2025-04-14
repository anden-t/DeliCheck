using System.Text.Json.Serialization;

namespace DeliCheck.Models
{
    /// <summary>
    /// Модель счета, выставленного пользователю
    /// </summary>
    public class BillModel
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Незаристривован ли пользователь, которому выставлен счет (true - незарегестрирован, false - зарегистрирован)
        /// </summary>
        public bool OfflineOwner { get; set; }
        /// <summary>
        /// Идентификатор пользователя, которому выставлен счет. Если offline_owner = true, то это friendLabelId, если false - userId
        /// </summary>
        public int OwnerId { get; set; }
        /// <summary>
        /// Идентификатор чека, по которому выставлен счет
        /// </summary>
        public int InvoiceId { get; set; }
        /// <summary>
        /// Сумма
        /// </summary>
        public decimal TotalCost { get; set; }
        /// <summary>
        /// Оплачен ли счет
        /// </summary>
        public bool Payed { get; set; }
    }
}
