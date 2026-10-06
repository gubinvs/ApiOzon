using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryCheckoutResponse
    {
        [JsonPropertyName("results")]
        public List<CheckoutResult> Results { get; set; } = new();
    }
}