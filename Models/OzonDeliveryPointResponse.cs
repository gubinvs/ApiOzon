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
}
