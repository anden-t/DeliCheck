using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    public class FriendResponseModel
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
        public int FriendLabelId { get; set; }
        /// <summary>
        /// Есть ли аватар
        /// </summary>
        [JsonPropertyName("has_avatar")]
        public bool HasAvatar { get; set; }
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

    }
}
