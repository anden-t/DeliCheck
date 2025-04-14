namespace DeliCheck.Models
{
    /// <summary>
    /// Моедль чека
    /// </summary>
    public class InvoiceModel
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Идентификатор пользователя, который отсканировал чек
        /// </summary>
        public int OwnerId { get; set; }
        /// <summary>
        /// Время, когда был отсканирован чек (UTC)
        /// </summary>
        public DateTime CreatedTime{ get; set; }
        /// <summary>
        /// Название чека
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Итоговая сумма чека
        /// </summary>
        public decimal TotalCost { get; set; }
        /// <summary>
        /// Отсанированный чек. Пустая строка, если распознано с помощью налоговой
        /// </summary>
        public string OCRText { get; set; }
        /// <summary>
        /// Показывает, распознано ли с помощью налоговой. True - если да
        /// </summary>
        public bool FromFns { get; set; }
        /// <summary>
        /// Созданы ли счета по этому чеку. Если true, изменения чека больше недоступны
        /// </summary>
        public bool BillsCreated { get; set; }
    }
}
