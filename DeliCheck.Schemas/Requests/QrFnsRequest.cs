using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    public class QrFnsRequest
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
