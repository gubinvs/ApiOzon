using System.Text.Json.Serialization;


namespace ApiOzon.Models
{
     public class Recipient
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}