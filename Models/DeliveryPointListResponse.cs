using System.Text.Json.Serialization;
using ApiOzon;

public class DeliveryPointListResponse
{
    [JsonPropertyName("delivery_points")]
    public List<DeliveryPointListItem> DeliveryPoints { get; set; } = new();

    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; set; }

    [JsonPropertyName("pagination")]
    public PaginationInfo? Pagination { get; set; }
}


public class DeliveryPointListItem
{
    [JsonPropertyName("delivery_point_id")]
    public long DeliveryPointId { get; set; }

   [JsonPropertyName("delivery_point_number")]
    public string DeliveryPointNumber { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string FullAddress { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public double? Latitude { get; set; }

    [JsonPropertyName("lng")]
    public double? Longitude { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("shipment_method_id")]
    public List<long> ShipmentMethodIds { get; set; } = new();
}