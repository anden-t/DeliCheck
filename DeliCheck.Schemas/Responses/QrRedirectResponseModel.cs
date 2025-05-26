using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    public class QrRedirectResponseModel
    {
        [JsonPropertyName("relative_url")]
        public string RelativeUrl { get; set; }
    }
}
