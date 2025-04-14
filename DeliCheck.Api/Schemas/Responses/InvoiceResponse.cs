using DeliCheck.Models;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    public class InvoiceResponse : ResponseBase
    {
        [JsonPropertyName("invoice")]
        public InvoiceResponseModel Invoice { get; set; }
    }
}
