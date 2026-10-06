using System.Text.Json.Serialization;


namespace ApiOzon.Models
{
    public class DeliveryOrderCreateResponse
    {
        [JsonPropertyName("order_number")]
        public string OrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("order_external_id")]
        public string OrderExternalId { get; set; } = string.Empty;

        [JsonPropertyName("postings")]
        public List<CreatedPosting> Postings { get; set; } = new();
    }

}