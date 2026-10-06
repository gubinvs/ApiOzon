using System.Text.Json.Serialization;


namespace ApiOzon.Models
{
    
    public class OzonDeliveryPointItem
    {
        [JsonPropertyName("delivery_point_id")]
        public long DeliveryPointId { get; set; }

        [JsonPropertyName("shipment_method_ids")]
        public List<long> ShipmentMethodIds { get; set; } = new();
    }
}