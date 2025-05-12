namespace DeliCheck.Schemas
{
    /// <summary>
    /// Тип деления чека
    /// </summary>
    public enum InvoiceSplitType
    {
        /// <summary>
        /// Позиции выбирает владелец чека (тот, кто отсканировал)
        /// </summary>
        ByOwner,
        /// <summary>
        /// Каждый участник трапезы выбирает свои позиции сам
        /// </summary>
        ByMembers
    }
}
