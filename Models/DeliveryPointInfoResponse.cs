using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryPointInfoResponse
    {
        [JsonPropertyName("delivery_points")]
        public List<DeliveryPointInfo> DeliveryPoints { get; set; } = new();
    }
}
