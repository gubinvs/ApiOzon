

namespace ApiOzon.Core
{
    public class OzonPayParam
    {
        public string BaseUrl {set; get;} = string.Empty;

        public string AccessKey {get; set;} =  string.Empty;

        public string SecretKey {set; get;} = string.Empty;

        public string NotificationSecretKey {set; get;} = string.Empty;
    }
}