

namespace ApiOzon.Core
{
    public class OzonPayParam
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string NotificationSecretKey { get; set; } = string.Empty;

        // Новые поля, которые зачитаются из вашего конфига
        public string FailUrl { get; set; } = string.Empty;
        public string SuccessUrl { get; set; } = string.Empty;
        public string NotificationUrl { get; set; } = string.Empty;
        public string ReceiptEmail { get; set; } = string.Empty;
    }
}
