using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DeliCheck.Schemas.SignalR.Responses
{
    /// <summary>
    /// Сообщение SignalR о завершении процесса деления чека
    /// </summary>
    public class InvoiceSplittingFinishedResponse
    {
        /// <summary>
        /// Имя метода
        /// </summary>
        public const string MethodName = "Finished";
        /// <summary>
        /// Идентификатор чека
        /// </summary>
        [JsonPropertyName("invoice_id")]
        public int InvoiceId { get; set; }
    }
}
