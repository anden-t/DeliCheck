using DeliCheck.Schemas.Responses;
using System.Text.Json.Serialization;

namespace DeliCheck.Web.Models
{
    public class InvoiceItem
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
        [JsonPropertyName("count")]
        public decimal Count { get; set; }
        /// <summary>
        /// Стоимость всей позиции (всего количества)
        /// </summary>
        [JsonPropertyName("cost")]
        public int Cost { get; set; }

        public bool IsEditingName { get; set; }
        public bool IsEditingCost { get; set; }
        //public bool IsEditingCount { get; set; }

        public string EditedName { get; set; }
        public int EditedCost { get; set; }
        //public decimal EditedCount { get; set; }

        public bool EnableToSaveName { get; set; }
        public bool EnableToSaveCost { get; set; }
       // public bool EnableToSaveCount { get; set; }

        public bool SavingName { get; set; }
        public bool SavingCost { get; set; }
        public bool SavingDecreaseCount { get; set; }
        public bool SavingIncreaseCount { get; set; }

        public bool CountInteger => Count % 1 == 0;

        public bool Deleting { get; set; }

        public Dictionary<Friend, int> UsersParts { get; set; }


        public InvoiceItem()
        {
            UsersParts = new Dictionary<Friend, int>();
        }

        public static InvoiceItem FromModel(InvoiceItemResponseModel model)
        {
            return new InvoiceItem()
            {
                Id = model.Id,
                Name = model.Name,
                Count = model.Quantity,
                Cost = (int)Math.Round(model.Cost),
            };
        }

        public static List<InvoiceItem> FromList(List<InvoiceItemResponseModel> list) => list.Select(x => FromModel(x)).ToList();
    }

    public static class InvoiceItemExtenstions
    {
        public static InvoiceItem FromModel(this InvoiceItemResponseModel model) => InvoiceItem.FromModel(model);
        public static List<InvoiceItem> FromList(this List<InvoiceItemResponseModel> list) => InvoiceItem.FromList(list);
    }
}
