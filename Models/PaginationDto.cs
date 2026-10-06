using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class PaginationDto
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }
}
