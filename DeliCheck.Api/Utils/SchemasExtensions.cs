using DeliCheck.Models;
using DeliCheck.Schemas.Responses;

namespace DeliCheck.Utils
{
    public static class SchemasExtensions
    {
        public static FriendResponseModel Deleted(IConfiguration configuration) => new FriendResponseModel()
        {
            AvatarUrl = $"https://{configuration["Domain"]}/avatars/default",
            Firstname = "Deleted",
            Lastname = "Deleted",
            FriendLabelId = null,
            UserId = null,
            HasProfile = false,
            HasAvatar = false,
            HasVk = false,
            VkId = null
        };

        /// <summary>
        /// Получает информацию о счете по моделе
        /// </summary>
        /// <param name="model">Модель</param>
        /// <param name="db">База данных</param>
        /// <param name="configuration">Конфигурация</param>
        /// <returns></returns>
        public static BillResponseModel ToResponseModel(this BillModel model, DatabaseContext db, IConfiguration configuration)
        {
            FriendResponseModel? owner = null, invoiceOwner = null;

            var invoice = db.Invoices.FirstOrDefault(x => x.Id == model.InvoiceId);
            if (invoice != null)
                invoiceOwner = db.Users.FirstOrDefault(x => x.Id == invoice.OwnerId)?.ToFriendResponseModel(configuration);

            if (model.OfflineOwner)
                owner = db.OfflineFriends.FirstOrDefault(x => x.Id == model.OwnerId)?.ToFriendResponseModel(configuration);
            else
                owner = db.Users.FirstOrDefault(x => x.Id == model.OwnerId)?.ToFriendResponseModel(configuration);

            if (owner == null)
                owner = Deleted(configuration);
            if (invoiceOwner == null)
                invoiceOwner = Deleted(configuration);

            return new BillResponseModel()
            {
                Id = model.Id,
                InvoiceId = model.InvoiceId,
                Items = db.BillsItems.Where(x => x.BillId == model.Id).Select(x => new BillItemResponseModel() { Cost = x.Cost, Quantity = x.Quantity, Name = x.Name }).ToList(),
                OfflineOwner = model.OfflineOwner,
                OwnerId = model.OwnerId,
                Payed = model.Payed,
                TotalCost = model.TotalCost,
                Owner = owner,
                InvoiceOwner = invoiceOwner
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
                Items = db.InvoicesItems.Where(x => x.InvoiceId == model.Id).Select(x => new InvoiceItemResponseModel() { Cost = x.Cost, Id = x.Id, Quantity = x.Quantity, Name = x.Name }).ToList(),
                Name = model.Name,
                OcrText = model.OcrText,
                OcrEngine = model.OcrEngine,
                TotalCost = model.TotalCost,
                EditingFinished = model.EditingFinished,
                SplitType = model.SplitType,
                OwnerId = model.OwnerId
            };
        }

        /// <summary>
        /// Получить информацию о пользователе
        /// </summary>
        /// <param name="model">Модель</param>
        /// <param name="self">Свой профиль или нет</param>
        /// <returns></returns>
        public static ProfileResponseModel ToProfileResponseModel(this UserModel model, IConfiguration configuration)
        {
            return new ProfileResponseModel()
            {
                Email = model.Email,
                Firstname = model.Firstname,
                HasAvatar = model.HasAvatar,
                Lastname = model.Lastname,
                PhoneNumber = model.PhoneNumber,
                Id = model.Id,
                Username = model.Username,
                VkId = model.VkId,
                AvatarUrl = model.HasAvatar ? $"https://{configuration["Domain"]}/avatars/user?userId={model.Id}" : $"https://{configuration["Domain"]}/avatars/default",
            };
        }

        /// <summary>
        /// Получить информацию о пользователе
        /// </summary>
        /// <param name="model">Модель</param>
        /// <param name="configuration">Конфигурация</param>
        /// <returns></returns>
        public static FriendResponseModel ToFriendResponseModel(this UserModel model, IConfiguration configuration)
        {
            return new FriendResponseModel()
            {
                Firstname = model.Firstname,
                HasAvatar = model.HasAvatar,
                Lastname = model.Lastname,
                UserId = model.Id,
                FriendLabelId = null,
                AvatarUrl = model.HasAvatar ? $"https://{configuration["Domain"]}/avatars/user?userId={model.Id}" : $"https://{configuration["Domain"]}/avatars/default", 
                HasProfile = true,
                VkId = model.VkId,
                HasVk = model.VkId != null
            };
        }

        /// <summary>
        /// Получить информацию о пользователе
        /// </summary>
        /// <param name="model">Модель</param>
        /// <param name="configuration">Конфигурация</param>
        /// <returns></returns>
        public static FriendResponseModel ToFriendResponseModel(this OfflineFriendModel model, IConfiguration configuration)
        {
            return new FriendResponseModel()
            {
                Firstname = model.Firstname,
                HasAvatar = model.HasAvatar,
                Lastname = model.Lastname,
                UserId = null,
                FriendLabelId = model.Id,
                AvatarUrl = model.HasAvatar ? $"https://{configuration["Domain"]}/avatars/friends?userId={model.Id}" : $"https://{configuration["Domain"]}/avatars/default",
                HasProfile = false,
                VkId = model.VkId,
                HasVk = model.VkId != null
            };
        }
    }
}
