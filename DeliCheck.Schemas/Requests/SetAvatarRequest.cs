using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    public class SetAvatarRequest 
    {
        [JsonPropertyName("avatar_base64")]
        public string AvatarBase64 { get; set; }
    }
}
