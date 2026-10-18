
using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class UpdatingOrderFromIndividualsReguaest
    {
        [JsonPropertyName("id")]
        public int Id {get; set;}

        [JsonPropertyName("phone_user")]
        public string PhoneUser {get; set;} = string.Empty;

        [JsonPropertyName("status_order")]
        public string StatusOrder {get; set;} = string.Empty;

        [JsonPropertyName("amount")]
        public string? Amount {get; set;}

        [JsonPropertyName("date_time")]
        public string? DateTime {get; set;}

        [JsonPropertyName("product_name")]
        public string? ProductName {get; set;}
        
    }
}