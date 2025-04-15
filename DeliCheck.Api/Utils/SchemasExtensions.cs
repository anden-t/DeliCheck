using DeliCheck.Models;
using DeliCheck.Schemas.Responses;

namespace DeliCheck.Utils
{
    public static class SchemasExtensions
    {
        /// <summary>
        /// Получает информацию о счете по моделе
        /// </summary>
        /// <param name="model">Модель</param>
        /// <param name="db">База данных</param>
        /// <returns></returns>
        public static BillResponseModel ToResponseModel(this BillModel model, DatabaseContext db)
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
        /// Получить информацию о чеке
        /// </summary>
        /// <param name="model">Модель</param>
        /// <param name="db">База данных</param>
        /// <returns></returns>
        public static InvoiceResponseModel ToResponseModel(this InvoiceModel model, DatabaseContext db)
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
        /// Получить информацию о пользователе
        /// </summary>
        /// <param name="model">Модель</param>
        /// <param name="self">Свой профиль или нет</param>
        /// <returns></returns>
        public static ProfileResponseModel ToResponseModel(this UserModel model, bool self)
        {
            return new ProfileResponseModel()
            {
                Email = model.Email,
                Firstname = model.Firstname,
                HasAvatar = model.HasAvatar,
                Lastname = model.Lastname,
                PhoneNumber = model.PhoneNumber,
                Self = self, 
                Id = model.Id,
                Username = model.Username,
                VkId = model.VkId,
            };
        }
    }
}
