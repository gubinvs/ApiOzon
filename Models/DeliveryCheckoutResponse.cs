using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryCheckoutResponse
    {
        [JsonPropertyName("results")]
        public List<CheckoutResult> Results { get; set; } = new();
    }

    public class CheckoutResult
    {
        [JsonPropertyName("request_id")]
        public int RequestId { get; set; }

        [JsonPropertyName("posting")]
        public CheckoutPosting Posting { get; set; } = new();
    }

    public class CheckoutPosting
    {
        [JsonPropertyName("estimated_delivery_cost")]
        public Money EstimatedDeliveryCost { get; set; } = new();

        [JsonPropertyName("estimated_insurance_cost")]
        public Money EstimatedInsuranceCost { get; set; } = new();

        [JsonPropertyName("estimated_delivery_days")]
        public int EstimatedDeliveryDays { get; set; }

        [JsonPropertyName("cutoff_at")]
        public DateTime CutoffAt { get; set; }
    }

    public class Money
    {
        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; } = string.Empty;
    }
}