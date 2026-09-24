using System.Text.Json.Serialization;

namespace ApiOzon
{
    public class ConnectionDataShop
    {
        [JsonPropertyName("ConnectionDataString")]
        public string ConnectionDataString {get; set;} = string.Empty;
    }
}