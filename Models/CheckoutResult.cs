using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class CheckoutResult
    {
        [JsonPropertyName("request_id")]
        public int RequestId { get; set; }

        [JsonPropertyName("posting")]
        public CheckoutPosting Posting { get; set; } = new();
    }
}