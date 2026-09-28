using System.Text.Json.Serialization;

public class DeliveryPointSchedule
{
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("periods")]
    public List<DeliveryPointPeriod> Periods { get; set; } = new();
}

public class DeliveryPointPeriod
{
    [JsonPropertyName("from_local")]
    public TimeSpan FromLocal { get; set; }

    [JsonPropertyName("to_local")]
    public TimeSpan ToLocal { get; set; }
}