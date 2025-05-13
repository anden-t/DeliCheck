using DeliCheck.Schemas;
using DeliCheck.Schemas.Responses;
using DeliCheck.Schemas.SignalR;

namespace DeliCheck.Web.Models
{
    public class InvoiceItem
    {
        /// <summary>
        /// Идентификатор 
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Название позиции
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Количество в позиции
        /// </summary>
        public decimal Count { get; set; }
        /// <summary>
        /// Стоимость всей позиции (всего количества)
        /// </summary>
        public int Cost { get; set; }
        /// <summary>
        /// Мера количества
        /// </summary>
        public ItemQuantityMeasure QuantityMeasure { get; set; }

        public bool IsEditingName { get; set; }
        public bool IsEditingCost { get; set; }

        public string EditedName { get; set; }
        public int EditedCost { get; set; }

        public bool EnableToSaveName { get; set; }
        public bool EnableToSaveCost { get; set; }

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
                QuantityMeasure = model.QuantityMeasure,
                Cost = (int)Math.Round(model.Cost),
            };
        }

        public static InvoiceItem FromModel(SplittingItem model, List<Friend> users)
        {
            return new InvoiceItem()
            {
                Id = model.Id,
                Name = model.Name,
                Count = model.Quantity,
                QuantityMeasure = model.QuantityMeasure,
                Cost = (int)Math.Round(model.Cost),
                UsersParts = new Dictionary<Friend, int>(model.UserParts.Select(x => new KeyValuePair<Friend, int>(users.FirstOrDefault(c => x.Key == c.UserId), x.Value)))
            };
        }

        public static List<InvoiceItem> FromList(List<InvoiceItemResponseModel> list) => list.Select(x => FromModel(x)).ToList();
    }

    public static class InvoiceItemExtenstions
    {
        public static InvoiceItem FromModel(this InvoiceItemResponseModel model) => InvoiceItem.FromModel(model);
        public static InvoiceItem FromModel(this SplittingItem model, List<Friend> users) => InvoiceItem.FromModel(model, users);
        public static List<InvoiceItem> FromList(this List<InvoiceItemResponseModel> list) => InvoiceItem.FromList(list);
    }
}
