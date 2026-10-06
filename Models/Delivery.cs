using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
        public class Delivery
    {
        [JsonPropertyName("delivery_point")]
        public DeliveryPoint DeliveryPoint { get; set; } = new();
    }
}