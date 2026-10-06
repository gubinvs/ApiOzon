using System.Text.Json.Serialization;


namespace ApiOzon.Models
{
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
}