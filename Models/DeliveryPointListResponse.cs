using System.Text.Json.Serialization;

public class DeliveryPointListResponse
{
    [JsonPropertyName("delivery_points")]
    public List<DeliveryPointListItem> DeliveryPoints { get; set; } = new();
}

public class DeliveryPointListItem
{
    [JsonPropertyName("delivery_point_id")]
    public long DeliveryPointId { get; set; }

    [JsonPropertyName("shipment_method_ids")]
    public List<long> ShipmentMethodIds { get; set; } = new();
}