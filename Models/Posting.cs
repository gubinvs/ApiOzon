using System.Text.Json.Serialization;



namespace ApiOzon.Models
{
       public class Posting
    {
        [JsonPropertyName("request_id")]
        public int RequestId { get; set; }

        [JsonPropertyName("shipment_method_id")]
        public long ShipmentMethodId { get; set; }

        [JsonPropertyName("cutoff_at")]
        public DateTime CutoffAt { get; set; }

        [JsonPropertyName("declared_value")]
        public DeclaredValue DeclaredValue { get; set; } = new();

        [JsonPropertyName("dimensions")]
        public Dimensions Dimensions { get; set; } = new();
    }
}