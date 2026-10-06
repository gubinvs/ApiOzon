using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryCheckoutRequest
    {
        [JsonPropertyName("recipient")]
        public Recipient Recipient { get; set; } = new();

        [JsonPropertyName("postings")]
        public List<Posting> Postings { get; set; } = new();

        [JsonPropertyName("delivery")]
        public Delivery Delivery { get; set; } = new();
    }

}