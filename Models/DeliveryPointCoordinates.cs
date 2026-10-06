using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryPointCoordinates
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }
}
