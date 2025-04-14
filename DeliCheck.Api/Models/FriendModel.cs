namespace DeliCheck.Models
{
    /// <summary>
    /// Представление друга
    /// </summary>
    public class FriendModel
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Идентификатор пользователя, кто добавил <see cref="FriendId"/> в друзья
        /// </summary>
        public int OwnerId { get; set; }
        /// <summary>
        /// Идентификатор того пользователя, кого добавил <see cref="OwnerId"/> в друзья
        /// </summary>
        public int FriendId { get; set; }
    }
}
