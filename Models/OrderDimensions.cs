using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
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
  