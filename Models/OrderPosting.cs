    using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class OrderPosting
    {
        [JsonPropertyName("request_id")]
        public int RequestId { get; set; }

        [JsonPropertyName("posting_external_id")]
        public string PostingExternalId { get; set; } = string.Empty;

        [JsonPropertyName("shipment_method_id")]
        public long ShipmentMethodId { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("declared_value")]
        public OrderDeclaredValue DeclaredValue { get; set; } = new();

        [JsonPropertyName("cutoff_at")]
        public DateTime CutoffAt { get; set; }

        [JsonPropertyName("dimensions")]
        public OrderDimensions Dimensions { get; set; } = new();
    }
}
    
    
