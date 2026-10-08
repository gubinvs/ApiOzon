using System.Text.Json.Serialization;


namespace ApiOzon.Models
{
    public class SberRegisterResponse
    {
        [JsonPropertyName("orderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("formUrl")]
        public string FormUrl { get; set; } = string.Empty;

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
        
        [JsonPropertyName("errorCode")]
        public string ErrorCode { get; set; } = string.Empty;
    }
}