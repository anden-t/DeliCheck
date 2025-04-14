using System.Text.Json.Serialization;

namespace DeliCheck.Models
{
    public class VkUserModel
    {
        [JsonPropertyName("first_name")]
        public string Firstname { get; set; }
        [JsonPropertyName("last_name")]
        public string Lastname { get; set; }
        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }
    }
}
