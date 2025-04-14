using System.Text.Json.Serialization;

namespace DeliCheck.Models
{
    public class VkAuthorizationData
    {
        public int Id { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("user_id")]
        public int VkUserId { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }
        public string DeviceId { get; set; }
        public DateTime CreateTime { get; set; }
        public int UserId { get; set; }
    }
}
