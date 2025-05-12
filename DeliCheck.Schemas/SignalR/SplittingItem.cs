using DeliCheck.Schemas.Responses;

namespace DeliCheck.Schemas.SignalR
{
    /// <summary>
    /// Позиция для деления
    /// </summary>
    public class SplittingItem
    {
        /// <summary>
        /// Идентификатор позиции
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Имя позиции
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Количество
        /// </summary>
        public decimal Quantity { get; set; }
        /// <summary>
        /// Стоимость
        /// </summary>
        public decimal Cost { get; set; }
        /// <summary>
        /// Мера количества
        /// </summary>
        public ItemQuantityMeasure QuantityMeasure { get; set; }
        /// <summary>
        /// Части пользователей
        /// </summary>
        public Dictionary<int, int> UserParts { get; set; }
    }
}
