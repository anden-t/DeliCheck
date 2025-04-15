using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Ответ со списком друзей
    /// </summary>
    public class FriendsListResponseModel
    {
        /// <summary>
        /// Список друзей в алфавитном порядке
        /// </summary>
        [JsonPropertyName("friends")]
        public List<FriendResponseModel> Friends { get; set; }

        /// <summary>
        /// Идентификатор пользователя, у которого получаем список друзей
        /// </summary>
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }
        /// <summary>
        /// Получаем ли список только зарегистрированных друзей
        /// </summary>
        [JsonPropertyName("only_online")]
        public bool OnlyOnline { get; set; }
    }
}
