
using DeliCheck.Models;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Информация о чеке
    /// </summary>
    public class InvoiceResponseModel
    {
        public static InvoiceResponseModel FromModel(InvoiceModel model, DatabaseContext db)
        {
            return new InvoiceResponseModel() 
            {
                BillsCreated = model.BillsCreated,
                CreatedTime = model.CreatedTime,
                Id = model.Id,
                FromFns = model.FromFns,
                Items = db.InvoicesItems.Where(x => x.InvoiceId == model.Id).Select(x => new InvoiceItemResponseModel() { Cost = x.Cost, Id = x.Id, Count = x.Count, Name = x.Name }).ToList(),
                Name = model.Name,
                OCRText = model.OCRText,
                TotalCost = model.TotalCost,
                OwnerId = model.OwnerId
            };
        }

        /// <summary>
        /// Идентификатор
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }
        /// <summary>
        /// Идентификатор пользователя, который отсканировал чек
        /// </summary>
        [JsonPropertyName("owner_id")]
        public int OwnerId { get; set; }
        /// <summary>
        /// Время, когда был отсканирован чек (UTC)
        /// </summary>
        [JsonPropertyName("created_time")]
        public DateTime CreatedTime { get; set; }
        /// <summary>
        /// Название чека
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }
        /// <summary>
        /// Итоговая сумма чека
        /// </summary>
        [JsonPropertyName("total_cost")]
        public decimal TotalCost { get; set; }
        /// <summary>
        /// Позиции в чеке
        /// </summary>
        [JsonPropertyName("items")]
        public List<InvoiceItemResponseModel> Items { get; set; }
        /// <summary>
        /// Отсанированный чек. Пустая строка, если распознано с помощью налоговой
        /// </summary>
        [JsonPropertyName("ocr_text")]
        public string OCRText { get; set; }
        /// <summary>
        /// Показывает, распознано ли с помощью налоговой. True - если да
        /// </summary>
        [JsonPropertyName("from_fns")]
        public bool FromFns { get; set; }
        /// <summary>
        /// Созданы ли счета по этому чеку. Если true, изменения чека больше недоступны
        /// </summary>
        [JsonPropertyName("bills_created")]
        public bool BillsCreated { get; set; }
    }
}
