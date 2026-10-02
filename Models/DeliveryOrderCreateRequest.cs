using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryOrderCreateRequest
    {
        [JsonPropertyName("order_external_id")]
        public string OrderExternalId { get; set; } = string.Empty;

        [JsonPropertyName("recipient")]
        public OrderRecipient Recipient { get; set; } = new();

        [JsonPropertyName("delivery")]
        public OrderDelivery Delivery { get; set; } = new();

        [JsonPropertyName("postings")]
        public List<OrderPosting> Postings { get; set; } = new();
    }

    public class OrderRecipient
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;
    }

    public class OrderDelivery
    {
        [JsonPropertyName("delivery_point")]
        public OrderDeliveryPoint DeliveryPoint { get; set; } = new();
    }

    public class OrderDeliveryPoint
    {
        [JsonPropertyName("delivery_point_id")]
        public int DeliveryPointId { get; set; }
    }

    public class OrderPosting
    {
        [JsonPropertyName("request_id")]
        public int RequestId { get; set; }

        [JsonPropertyName("posting_external_id")]
        public string PostingExternalId { get; set; } = string.Empty;

        [JsonPropertyName("shipment_method_id")]
        public long ShipmentMethodId { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("declared_value")]
        public OrderDeclaredValue DeclaredValue { get; set; } = new();

        [JsonPropertyName("cutoff_at")]
        public DateTime CutoffAt { get; set; }

        [JsonPropertyName("dimensions")]
        public OrderDimensions Dimensions { get; set; } = new();
    }

    public class OrderDeclaredValue
    {
        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; } = "RUB";
    }

    public class OrderDimensions
    {
        [JsonPropertyName("weight_g")]
        public int WeightG { get; set; }

        [JsonPropertyName("length_mm")]
        public int LengthMm { get; set; }

        [JsonPropertyName("width_mm")]
        public int WidthMm { get; set; }

        [JsonPropertyName("height_mm")]
        public int HeightMm { get; set; }
    }
}