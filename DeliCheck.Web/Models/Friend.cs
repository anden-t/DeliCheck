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
        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }
        /// <summary>
        /// Фамилия
        /// </summary>
        [JsonPropertyName("lastname")]
        public string Lastname { get; set; }
        /// <summary>
        /// Имя
        /// </summary>
        [JsonPropertyName("firstname")]
        public string Firstname { get; set; }
        /// <summary>
        /// Идентификатор записи о друге
        /// </summary>
        [JsonPropertyName("friend_label_id")]
        public int? FriendLabelId { get; set; }
        /// <summary>
        /// Есть ли аватар
        /// </summary>
        [JsonPropertyName("has_avatar")]
        public bool HasAvatar { get; set; }
        /// <summary>
        /// Url аватара
        /// </summary>
        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }
        /// <summary>
        /// Зарегистрирован ли пользователя
        /// </summary>
        [JsonPropertyName("has_profile")]
        public bool HasProfile { get; set; }
        /// <summary>
        /// Привязан ли VK
        /// </summary>
        [JsonPropertyName("has_vk")]
        public bool HasVk { get; set; }
        /// <summary>
        /// Идентификатор пользователя в VK
        /// </summary>
        [JsonPropertyName("vk_id")]
        public long? VkId { get; set; }

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

        public static List<Friend> FromList(List<FriendResponseModel> list) => list.Select(x => FromModel(x)).ToList();
    }

    public static class FriendExtenstions
    {
        public static Friend FromModel(this FriendResponseModel model) => Friend.FromModel(model);
        public static List<Friend> FromList(this List<FriendResponseModel> list) => Friend.FromList(list);
    }
}
