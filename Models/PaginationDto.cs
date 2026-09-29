using System.Text.Json.Serialization;

namespace ApiOzon.DTOs // или укажите вашу рабочую папку для DTO
{
    public class PaginationDto
    {
        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }
}
