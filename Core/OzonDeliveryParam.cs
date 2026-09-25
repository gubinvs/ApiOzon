using System.Text.Json.Serialization;

namespace ApiOzon
{
    public class OzonDeliveryParam
    {
        // Имена должны в точности повторять ключи из appsettings.json
        public string auth_url { get; set; } = string.Empty;
        public string host { get; set; } = string.Empty;
        public string client_id { get; set; } = string.Empty;
        public string client_secret { get; set; } = string.Empty;
    }
}