using DeliCheck.Schemas.Responses;

namespace DeliCheck.Schemas.SignalR
{
    /// <summary>
    /// Стостояние деления чека
    /// </summary>
    public class InvoiceSplittingModel
    {
        /// <summary>
        /// Идентификатор чека
        /// </summary>
        public int InvoiceId { get; set; }
        /// <summary>
        /// Список позиций
        /// </summary>
        public List<SplittingItem> Items { get; set; }
        /// <summary>
        /// Список пользователей
        /// </summary>
        public List<ProfileResponseModel> Users { get; set; }
        /// <summary>
        /// Список идентификаторов пользователей, которые закончили выбор позиций
        /// </summary>
        public List<int> FinishedUsers { get; set; }
        /// <summary>
        /// Завершено ли деление чека
        /// </summary>
        public bool IsFinished { get; set; }
    }
}
