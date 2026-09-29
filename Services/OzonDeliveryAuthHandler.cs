using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ApiOzon.Services
{
    public class OzonDeliveryAuthHandler : DelegatingHandler
    {
        private readonly OzonDeliveryParam _config;
        
        // Потокобезопасный кэш токена в памяти приложения
        private static string? _cachedToken;
        private static DateTime _tokenExpiry = DateTime.MinValue;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public OzonDeliveryAuthHandler(IOptions<OzonDeliveryParam> options)
        {
            _config = options.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 1. Получаем JWT-токен (из кэша или запрашиваем новый)
            string token = await GetAccessTokenAsync(cancellationToken);

            // 2. Подставляем токен в заголовок Authorization
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 3. Пробрасываем запрос дальше к api-delivery.ozon.ru
            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            if (_cachedToken != null && _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
            {
                return _cachedToken;
            }

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_cachedToken != null && _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
                {
                    return _cachedToken;
                }

                // Чистый HttpClient без middleware во избежание рекурсии
                using var authClient = new HttpClient();

                var tokenRequestBody = new
                {
                    grant_type = "client_credentials",
                    client_id = _config.client_id,
                    client_secret = _config.client_secret
                };

                var response = await authClient.PostAsJsonAsync(_config.auth_url, tokenRequestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Ошибка Ozon OAuth (Статус {response.StatusCode}): {errorContent}");
                }

                var tokenData = await response.Content.ReadFromJsonAsync<OzonDeliveryTokenResponse>(cancellationToken: cancellationToken);

                if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
                {
                    throw new Exception("Ozon OAuth вернул пустой токен.");
                }

                _cachedToken = tokenData.AccessToken;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn > 0 ? tokenData.ExpiresIn : 3600);

                return _cachedToken;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    public class OzonDeliveryTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
