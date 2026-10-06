using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class PaginationInfo
    {
        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }
    }
}