using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    public class InvoiceEditRequest
    {
        [Required]
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [Required]
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [Required]
        [JsonPropertyName("items")]
        public List<InvoiceEditItem>? Items { get; set; }
    }
}
