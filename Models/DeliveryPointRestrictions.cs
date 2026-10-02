using System.Text.Json.Serialization;
using ApiOzon.Models;

public class DeliveryPointRestrictions
{
    [JsonPropertyName("max_weight_g")]
    public int MaxWeightG { get; set; }

    [JsonPropertyName("max_width_mm")]
    public int MaxWidthMm { get; set; }

    [JsonPropertyName("max_length_mm")]
    public int MaxLengthMm { get; set; }

    [JsonPropertyName("max_height_mm")]
    public int MaxHeightMm { get; set; }

    [JsonPropertyName("max_price")]
    public Money MaxPrice { get; set; } = new();
}