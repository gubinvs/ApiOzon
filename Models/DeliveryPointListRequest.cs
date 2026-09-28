using System.Text.Json.Serialization;

public class DeliveryPointListRequest
{
    [JsonPropertyName("type")]
    public List<string> Type { get; set; } = new();

    [JsonPropertyName("pagination")]
    public DeliveryPointPagination Pagination { get; set; } = new();
}

public class DeliveryPointPagination
{
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}