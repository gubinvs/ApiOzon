using System.Text.Json.Serialization;


namespace ApiOzon.Models
{
    public class DeliveryPointCheckAvailabilityResponse
    {
        [JsonPropertyName("results")]
        public PointCheckAvailability? PointCheckAvailability {get; set;}
    }
}