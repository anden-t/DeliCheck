using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    public class QrFnsRequest
    {
        [JsonPropertyName("qr_code_text")]
        public string QrCodeText { get; set; }
    }
}
