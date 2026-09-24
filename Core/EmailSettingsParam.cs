using System.Text.Json.Serialization;


namespace ApiOzon
{
    public class EmailSettingsParam
    {
        [JsonPropertyName("SmtpServer")]
        public string SmtpServer {get; set;} = string.Empty;

        [JsonPropertyName("Port")]
        public string Port {get; set;} = string.Empty;

        [JsonPropertyName("FromEmail")]
        public string FromEmail {get; set;} = string.Empty;

        [JsonPropertyName("Password")]
        public string Password {get; set;} = string.Empty;
    }
}