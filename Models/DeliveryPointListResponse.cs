using System.Text.Json.Serialization;


namespace ApiOzon.Models
{
    public class DeliveryPointListResponse
    {
        [JsonPropertyName("delivery_points")]
        public List<DeliveryPointListItem> DeliveryPoints { get; set; } = new();

        [JsonPropertyName("next_cursor")]
        public string? NextCursor { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationInfo? Pagination { get; set; }
    }
}