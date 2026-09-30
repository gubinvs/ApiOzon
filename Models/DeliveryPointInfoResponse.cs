using System.Text.Json.Serialization;


public class DeliveryPointInfoResponse
{
    [JsonPropertyName("delivery_points")]
    public List<DeliveryPointInfo> DeliveryPoints { get; set; } = new();
}

public class DeliveryPointInfo
{
    [JsonPropertyName("delivery_point_id")]
    public long DeliveryPointId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("delivery_point_number")]
    public string DeliveryPointNumber { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("full_address")]
    public string FullAddress { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public DeliveryPointCoordinates Coordinates { get; set; } = new();

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("storage_period_days")]
    public int StoragePeriodDays { get; set; }

    [JsonPropertyName("fitting_rooms_count")]
    public int FittingRoomsCount { get; set; }

    [JsonPropertyName("is_bulky")]
    public bool IsBulky { get; set; }

    [JsonPropertyName("restrictions")]
    public DeliveryPointRestrictions Restrictions { get; set; } = new();
}
