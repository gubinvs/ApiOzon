using System.Text.Json.Serialization;

public class DeliveryPointInfoRequest
{
    [JsonPropertyName("delivery_point_ids")]
    public List<long> DeliveryPointIds { get; set; } = new();
}