using DeliCheck.Schemas.Responses;
using System.Diagnostics.Metrics;
using System.Text.Json.Serialization;

namespace DeliCheck.Web.Models
{
    public class Friend
    {
        /// <summary>
        /// Идентификатор пользователя. null, если пользователь не зарегистрирован (HasProfile = false)
        /// </summary>
        public int? UserId { get; set; }
        /// <summary>
        /// Фамилия
        /// </summary>
        public string Lastname { get; set; }
        /// <summary>
        /// Имя
        /// </summary>
        public string Firstname { get; set; }
        /// <summary>
        /// Идентификатор записи о друге
        /// </summary>
        public int? FriendLabelId { get; set; }
        /// <summary>
        /// Есть ли аватар
        /// </summary>
        public bool HasAvatar { get; set; }
        /// <summary>
        /// Url аватара
        /// </summary>
        public string AvatarUrl { get; set; }
        /// <summary>
        /// Зарегистрирован ли пользователя
        /// </summary>
        public bool HasProfile { get; set; }
        /// <summary>
        /// Привязан ли VK
        /// </summary>
        public bool HasVk { get; set; }
        /// <summary>
        /// Идентификатор пользователя в VK
        /// </summary>
        public long? VkId { get; set; }

        public bool Finished { get; set; }
        public bool Selected { get; set; }

        public static bool operator ==(Friend f1, Friend f2) => f1.HasProfile ? (f1.UserId == f2.UserId) : (f1.FriendLabelId == f2.FriendLabelId);
        public static bool operator !=(Friend f1, Friend f2) => !(f1.HasProfile ? (f1.UserId == f2.UserId) : (f1.FriendLabelId == f2.FriendLabelId));

        public override bool Equals(object? obj)
        {
            if (obj is Friend f)
                return HasProfile ? (UserId == f.UserId) : (FriendLabelId == f.FriendLabelId);

            return false;
        }

        public static Friend FromModel(FriendResponseModel model)
        {
            return new Friend()
            {
                UserId = model.UserId,
                AvatarUrl = model.AvatarUrl,
                HasAvatar = model.HasAvatar,
                Firstname = model.Firstname,
                FriendLabelId = model.FriendLabelId,
                HasProfile = model.HasProfile,
                HasVk = model.HasVk,
                Lastname = model.Lastname,
                VkId = model.VkId,
            };
        }

        public static Friend FromModel(ProfileResponseModel model)
        {
            return new Friend()
            {
                UserId = model.Id,
                AvatarUrl = model.AvatarUrl,
                HasAvatar = model.HasAvatar,
                Firstname = model.Firstname,
                Lastname = model.Lastname,
                VkId = model.VkId,
                HasProfile = true,
                HasVk = model.VkId != null,
                FriendLabelId = null
            };
        }

        public static List<Friend> FromList(List<FriendResponseModel> list) => list.Select(x => FromModel(x)).ToList();
    }

    public static class FriendExtenstions
    {
        public static Friend FromModel(this FriendResponseModel model) => Friend.FromModel(model);
        public static Friend FromModel(this ProfileResponseModel model) => Friend.FromModel(model);
        public static List<Friend> FromList(this List<FriendResponseModel> list) => Friend.FromList(list);
    }
}
