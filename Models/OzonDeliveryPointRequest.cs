using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class OzonDeliveryPointRequest
    {
        // Необязательные фильтры, которые поддерживает API Ozon
        [JsonPropertyName("card_payment")]
        public bool? CardPayment { get; set; }

        [JsonPropertyName("cash_payment")]
        public bool? CashPayment { get; set; }

        [JsonPropertyName("type")]
        public string[]? Type { get; set; } // Например: ["pickup", "postamat"]
    }
}