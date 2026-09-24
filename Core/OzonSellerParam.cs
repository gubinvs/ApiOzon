using System.Text.Json.Serialization;


namespace ApiOzon
{
    public class OzonSellerParam
    {
        [JsonPropertyName("UrlOzonApiAdress")]
        public string UrlOzonApiAdress {get; set;} = string.Empty;
    
        [JsonPropertyName("SellerClientId")]
        public int SellerClientId {get; set;}
        
        [JsonPropertyName("SellerApiKey")]
        public string SellerApiKey {get; set;} = string.Empty;
    }
}