using System.Text.Json.Serialization;


namespace ApiOzon.Models
{
    public class ErrorPointCheckAvailability
    {
        [JsonPropertyName("code")]
        public string Code {get; set;} = string.Empty;

        [JsonPropertyName("message")]
        public string Message {get; set;} = string.Empty;
    }
}