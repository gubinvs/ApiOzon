using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryPointPagination
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("cursor")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Cursor { get; set; }
    }
}

