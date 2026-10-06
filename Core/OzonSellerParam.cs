
namespace ApiOzon.Core
{
    public class OzonSellerParam
    {
        public string UrlOzonApiAdress {get; set;} = string.Empty;
        
        public int SellerClientId {get; set;}
        
        public string SellerApiKey {get; set;} = string.Empty;
    }
}