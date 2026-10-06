using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class Products
    {
        // Используем long, так как ID Ozon не влезают в стандартный int
        [JsonPropertyName("sku")]
        public long Sku { get; set; }

        [JsonPropertyName("offer_id")]
        public string OfferId { get; set; } = string.Empty;

        [JsonPropertyName("product_id")]
        public long ProductId { get; set; }

        [JsonPropertyName("warehouse_id")]
        public long WarehouseId { get; set; }

        [JsonPropertyName("present")]
        public int Present { get; set; }

        [JsonPropertyName("reserved")]
        public int Reserved { get; set; }
    }
}

