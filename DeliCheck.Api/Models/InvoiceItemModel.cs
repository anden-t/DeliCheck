using System.Text.Json.Serialization;

namespace DeliCheck.Models
{
    /// <summary>
    /// Модель позиции чека
    /// </summary>
    public class InvoiceItemModel
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
        public decimal Cost { get; set; }
        /// <summary>
        /// Идентификатор чека
        /// </summary>
        public int InvoiceId { get; set; }
    }
}
