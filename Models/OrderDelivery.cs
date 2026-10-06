using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class OrderDelivery
    {
        [JsonPropertyName("delivery_point")]
        public OrderDeliveryPoint DeliveryPoint { get; set; } = new();
    }
}

  