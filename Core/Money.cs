 
 using System.Text.Json.Serialization;
 public class Money
    {
        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; } = string.Empty;
    }