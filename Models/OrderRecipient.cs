using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    public class OrderRecipient
    {
        [JsonPropertyName("phone_number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = string.Empty;
    }
}