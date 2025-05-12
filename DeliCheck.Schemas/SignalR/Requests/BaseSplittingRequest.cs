using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.SignalR.Requests
{
    public class BaseSplittingRequest
    {
        [JsonPropertyName("invoice_id")]
        public int InvoiceId { get; set; }
        [JsonPropertyName("session_token")]
        public string SessionToken { get; set; }
    }
}
