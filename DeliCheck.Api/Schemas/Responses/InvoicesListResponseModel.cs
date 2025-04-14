using DeliCheck.Models;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    public class InvoicesListResponseModel : ResponseBase
    {
        [JsonPropertyName("list")]
        public List<InvoiceResponseModel> Invoices { get; set; }
    }
}
