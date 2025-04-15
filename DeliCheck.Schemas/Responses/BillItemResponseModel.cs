namespace DeliCheck.Schemas.Responses
{
    /// <summary>
    /// Позиция в счете
    /// </summary>
    public class BillItemResponseModel
    {
        /// <summary>
        /// Стоимость позиции (кол-во входит в стоимость, т. е. это цена за текущую Count)
        /// </summary>
        public decimal Cost { get; set; }
        /// <summary>
        /// Название позиции
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Количество в позиции
        /// </summary>
        public decimal Count { get; set; }
    }
}
