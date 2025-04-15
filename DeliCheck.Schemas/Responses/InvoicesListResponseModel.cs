using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    public class InvoicesListResponseModel
    {
        /// <summary>
        /// Список чеков
        /// </summary>
        [JsonPropertyName("list")]
        public List<InvoiceResponseModel> Invoices { get; set; }
    }
}
