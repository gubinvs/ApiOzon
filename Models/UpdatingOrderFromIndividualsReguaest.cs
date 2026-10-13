
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
        
    }
}