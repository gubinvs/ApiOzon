using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryPointCheckAvailabilityReguest
    {
        // Идентификатор ПВЗ назначения
        [JsonPropertyName("delivery_point_ids")]
        public List<int>? DeliveryPointIds {get; set;}

        // Номер пункта отправления ПВЗ
        [JsonPropertyName("shipment_method_id")]
        public long ShipmentMethodId {get; set;}

        [JsonPropertyName("postings")]
        public List<PostingAvailabilityReguest>? Postings {get; set;}
    }
}