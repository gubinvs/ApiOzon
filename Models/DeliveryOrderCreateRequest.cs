using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryOrderCreateRequest
    {
        [JsonPropertyName("order_external_id")]
        public string OrderExternalId { get; set; } = string.Empty;

        [JsonPropertyName("recipient")]
        public OrderRecipient Recipient { get; set; } = new();

        [JsonPropertyName("delivery")]
        public OrderDelivery Delivery { get; set; } = new();

        [JsonPropertyName("postings")]
        public List<OrderPosting> Postings { get; set; } = new();
    }
}