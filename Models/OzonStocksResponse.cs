using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class OzonStocksResponse
    {
        // Указываем точное имя из JSON и меняем тип на правильный список
        [JsonPropertyName("products")]
        public List<Products>? Products { get; set; }

        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }

        [JsonPropertyName("cursor")]
        public string Cursor { get; set; } = string.Empty;
    }
}