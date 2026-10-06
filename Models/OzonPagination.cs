using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class OzonPagination
    {
        // Количество пропускаемых элементов (0 — для первой страницы)
        [JsonPropertyName("offset")]
        public int Offset { get; set; } = 0;

        // Лимит элементов на страницу (ОБЯЗАТЕЛЬНОЕ ПОЛЕ ДЛЯ OZON)
        [JsonPropertyName("limit")]
        public int Limit { get; set; } = 50; 
    }
}