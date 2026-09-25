using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryCheckoutRequest
    {
        [JsonPropertyName("recipient")]
        public Recipient Recipient { get; set; } = new();

        [JsonPropertyName("postings")]
        public List<Posting> Postings { get; set; } = new();

        [JsonPropertyName("delivery")]
        public Delivery Delivery { get; set; } = new();
    }

    public class Recipient
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class Posting
    {
        [JsonPropertyName("request_id")]
        public int RequestId { get; set; }

        [JsonPropertyName("shipment_method_id")]
        public long ShipmentMethodId { get; set; }

        [JsonPropertyName("cutoff_at")]
        public DateTime CutoffAt { get; set; }

        [JsonPropertyName("declared_value")]
        public DeclaredValue DeclaredValue { get; set; } = new();

        [JsonPropertyName("dimensions")]
        public Dimensions Dimensions { get; set; } = new();
    }

    public class DeclaredValue
    {
        [JsonPropertyName("amount")]
        public string Amount { get; set; } = string.Empty;

        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; } = "RUB";
    }

    public class Dimensions
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

    public class Delivery
    {
        [JsonPropertyName("delivery_point")]
        public DeliveryPoint DeliveryPoint { get; set; } = new();
    }

    public class DeliveryPoint
    {
        [JsonPropertyName("delivery_point_id")]
        public int DeliveryPointId { get; set; }
    }
}