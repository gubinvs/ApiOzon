using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class OrderDeliveryPoint
    {
        [JsonPropertyName("delivery_point_id")]
        public int DeliveryPointId { get; set; }
    }
}

 
