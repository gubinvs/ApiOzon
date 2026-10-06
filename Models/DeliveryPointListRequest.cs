using System.Text.Json.Serialization;



namespace ApiOzon.Models
{
    public class DeliveryPointListRequest
    {
        [JsonPropertyName("type")]
        public List<string> Type { get; set; } = new();

        [JsonPropertyName("pagination")]
        public DeliveryPointPagination Pagination { get; set; } = new();
    }
}

