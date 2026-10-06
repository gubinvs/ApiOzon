using System.Text.Json.Serialization;


namespace ApiOzon.Models
{
    public class PointCheckAvailability
    {
        [JsonPropertyName("request_id")]
        public int RequestId {get; set;}

        [JsonPropertyName("cutoff_at")]
        public string CutoffAt {get; set;} = string.Empty;

        [JsonPropertyName("delivery_point_id")]
        public int DeliveryPointId {get; set;}

        [JsonPropertyName("error")]
        public ErrorPointCheckAvailability? Error {get; set;}
    }
}