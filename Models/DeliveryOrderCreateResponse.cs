using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryOrderCreateResponse
    {
        [JsonPropertyName("order_number")]
        public string OrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("order_external_id")]
        public string OrderExternalId { get; set; } = string.Empty;

        [JsonPropertyName("postings")]
        public List<CreatedPosting> Postings { get; set; } = new();
    }

   public class CreatedPosting
    {
        [JsonPropertyName("request_id")]
        public int RequestId { get; set; }

        [JsonPropertyName("posting_number")]
        public string PostingNumber { get; set; } = string.Empty;

        [JsonPropertyName("posting_external_id")]
        public string PostingExternalId { get; set; } = string.Empty;

        [JsonPropertyName("estimated_delivery_cost")]
        public Money? EstimatedDeliveryCost { get; set; }

        [JsonPropertyName("estimated_insurance_cost")]
        public Money? EstimatedInsuranceCost { get; set; }

        [JsonPropertyName("estimated_delivery_days")]
        public int EstimatedDeliveryDays { get; set; }

        [JsonPropertyName("cutoff_at")]
        public DateTime CutoffAt { get; set; }
    }
}