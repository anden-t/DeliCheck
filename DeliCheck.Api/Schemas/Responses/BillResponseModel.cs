using DeliCheck.Models;
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
        /// Получает информацию о счете по моделе
        /// </summary>
        /// <param name="model">Модель</param>
        /// <param name="ownerFirstname">Имя владельца счета</param>
        /// <param name="ownerLastname">Фамилия владельца счета</param>
        /// <returns></returns>
        public static BillResponseModel FromModel(BillModel model, DatabaseContext db)
        {
            string ownerFirstname, ownerLastname, invoiceOwnerFirstname, invoiceOwnerLastname;

            var invoice = db.Invoices.FirstOrDefault(x => x.Id == model.InvoiceId);
            if (invoice != null)
            {
                var invoiceOwner = db.Users.FirstOrDefault(x => x.Id == invoice.OwnerId);
                invoiceOwnerFirstname = invoiceOwner?.Firstname ?? string.Empty;
                invoiceOwnerLastname = invoiceOwner?.Lastname ?? string.Empty;
            }
            else invoiceOwnerFirstname = invoiceOwnerLastname = string.Empty;

            if (model.OfflineOwner)
            {
                var owner = db.OfflineFriends.FirstOrDefault(x => x.Id == model.OwnerId);
                ownerFirstname = owner?.Firstname ?? string.Empty;
                ownerLastname = owner?.Lastname ?? string.Empty;
            }
            else
            {
                var owner = db.Users.FirstOrDefault(x => x.Id == model.OwnerId);
                ownerFirstname = owner?.Firstname ?? string.Empty;
                ownerLastname = owner?.Lastname ?? string.Empty;
            }

            return new BillResponseModel()
            {
                Id = model.Id,
                InvoiceId = model.InvoiceId,
                Items = db.BillsItems.Where(x => x.BillId == model.Id).Select(x => new BillItemResponseModel() { Cost = x.Cost, Count = x.Count, Name = x.Name }).ToList(),
                OfflineOwner = model.OfflineOwner,
                OwnerId = model.OwnerId,
                Payed = model.Payed,
                TotalCost = model.TotalCost,
                OwnerFirstname = ownerFirstname,
                OwnerLastname = ownerLastname,
                InvoiceOwnerFirstname = invoiceOwnerFirstname,
                InvoiceOwnerLastname = invoiceOwnerLastname
            };
        }

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
        /// Имя пользователя, которому принадлежит счет
        /// </summary>
        [JsonPropertyName("owner_firstname")]
        public string OwnerFirstname { get; set; }
        /// <summary>
        /// Фамилия пользователя, которому принадлежит счет
        /// </summary>
        [JsonPropertyName("owner_lastname")]
        public string OwnerLastname { get; set; }
        /// <summary>
        /// Имя пользователя, которому принадлежит счет
        /// </summary>
        [JsonPropertyName("invoice_owner_firstname")]
        public string InvoiceOwnerFirstname { get; set; }
        /// <summary>
        /// Фамилия пользователя, которому принадлежит счет
        /// </summary>
        [JsonPropertyName("invoice_owner_lastname")]
        public string InvoiceOwnerLastname { get; set; }
    }
}
