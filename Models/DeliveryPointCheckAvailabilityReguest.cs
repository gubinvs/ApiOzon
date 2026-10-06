using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class DeliveryPointCheckAvailabilityReguest
    {
        // Идентификатор ПВЗ назначения
        [JsonPropertyName("delivery_point_ids")]
        public int DeliveryPointIds {get; set;}

        // Номер пункта отправления ПВЗ
        [JsonPropertyName("shipment_method_id")]
        public int ShipmentMethodId {get; set;}

        [JsonPropertyName("postings")]
        public PostingAvailabilityReguest? Postings {get; set;}
    }
}