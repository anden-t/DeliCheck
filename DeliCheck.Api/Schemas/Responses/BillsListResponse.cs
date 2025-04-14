using DeliCheck.Models;
using System.Text.Json.Serialization;

namespace DeliCheck.Schemas.Responses
{
    public class BillsListResponse : ResponseBase
    {
        [JsonPropertyName("bills")]
        public List<BillResponseModel> Bills { get; set; }
    }
}
