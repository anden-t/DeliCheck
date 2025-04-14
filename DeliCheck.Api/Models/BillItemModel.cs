﻿using System.Text.Json.Serialization;

namespace DeliCheck.Models
{
    /// <summary>
    /// Модель позиции у счета, выставленного пользователю (сколько он наел)
    /// </summary>
    public class BillItemModel
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public int Id { get; set; }
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
        /// <summary>
        /// Идентификатор счета, которому принадлежит позиция
        /// </summary>
        public int BillId { get; set; }
    }
}
