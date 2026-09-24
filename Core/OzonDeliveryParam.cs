using System.Text.Json.Serialization;


namespace ApiOzon
{
    public class OzonDeliveryParam
    {
        [JsonPropertyName("auth_url")]
        public string AuthUrl {get; set;} = string.Empty;

        [JsonPropertyName("host")]
        public string Host {get; set;} = string.Empty;

        [JsonPropertyName("client_id")]
        public string ClientId {get; set;} = string.Empty;

        [JsonPropertyName("client_secret")]
        public string ClientSecret {get; set;} = string.Empty;

    }
}