using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.SignalR.Responses
{
    /// <summary>
    /// Ответ SignalR об актуальном состоянии деления чека
    /// </summary>
    public class ActualInvoiceSplittingResponse
    {
        /// <summary>
        /// Имя метода
        /// </summary>
        public const string MethodName = "Actual";

        /// <summary>
        /// Модель деления чека
        /// </summary>
        [JsonPropertyName("actual")]
        public InvoiceSplittingModel SplittingModel { get; set; }
    }
}
