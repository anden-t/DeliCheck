using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    public class BillsListResponseModel
    {
        [JsonPropertyName("bills")]
        public List<BillResponseModel> Bills { get; set; }
    }
}
