using System.Text.Json.Serialization;

namespace ApiOzon
{
    public class PaginationInfo
    {
        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }
    }
}