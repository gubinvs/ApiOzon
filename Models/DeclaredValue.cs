using System.Text.Json.Serialization;


namespace ApiOzon.Models
{
     public class DeclaredValue
    {
        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; } = "RUB";
    }
}