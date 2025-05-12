using DeliCheck.Schemas.Responses;
using System.Text.Json.Serialization;

namespace DeliCheck.Web.Models
{
    public class UserBillItem
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
        public IQueryable<BillItemResponseModel> ItemsQueryable { get; set; }
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

        public string OwnerAvatarUrl { get; set; }


        public static UserBillItem FromModel(BillResponseModel bill)
        {
            return new UserBillItem()
            {
                Id = bill.Id,
                InvoiceId = bill.InvoiceId,
                InvoiceOwnerFirstname = bill.InvoiceOwner.Firstname,
                InvoiceOwnerLastname = bill.InvoiceOwner.Lastname,
                Items = bill.Items,
                OwnerId = bill.OwnerId,
                OfflineOwner = bill.OfflineOwner,
                OwnerFirstname = bill.Owner.Firstname,
                OwnerLastname = bill.Owner.Lastname,
                Payed = bill.Payed,
                TotalCost = bill.TotalCost,
                OwnerAvatarUrl = bill.Owner.AvatarUrl,
                ItemsQueryable = bill.Items.AsQueryable()
            };
        }
    }

    public static class UserBillItemExtensions
    {
        public static UserBillItem FromModel(this BillResponseModel bill) => UserBillItem.FromModel(bill);
        public static List<UserBillItem> FromList(this List<BillResponseModel> bill) => bill.Select(UserBillItem.FromModel).ToList();
    }
}
