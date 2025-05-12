using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Requests
{
    public class InvoiceEditItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }
    }
}
