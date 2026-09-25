using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class OzonDeliveryPointResponse
    {
        [JsonPropertyName("delivery_points")]
        public List<OzonDeliveryPointItem> DeliveryPoints { get; set; } = new();

        [JsonPropertyName("next_cursor")]
        public string? NextCursor { get; set; }
    }

    public class OzonDeliveryPointItem
    {
        [JsonPropertyName("delivery_point_id")]
        public long DeliveryPointId { get; set; }

        [JsonPropertyName("shipment_method_ids")]
        public List<long> ShipmentMethodIds { get; set; } = new();
    }
}
