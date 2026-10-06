using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ApiOzon.Core;

namespace ApiOzon.Services
{
    public interface IOzonAuthService
    {
        Task<string> GetTokenAsync();

        Task<string> RefreshTokenAsync();
    }


    public class OzonAuthService : IOzonAuthService
    {
        private readonly OzonDeliveryParam _deliveryParam;
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;

        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly object _lock = new object();


        public OzonAuthService(
            IOptions<OzonDeliveryParam> options)
        {
            _deliveryParam = options.Value;

            _cookieContainer = new CookieContainer();


            var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    CookieContainer = _cookieContainer
                };


            _httpClient = new HttpClient(handler);
        }

        // ==========================================================
        // Получение токена
        // ==========================================================

        public async Task<string> GetTokenAsync()
        {
            if (_cachedToken != null && _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
            {
                return _cachedToken;
            }

            lock (_lock)
            {
                if (_cachedToken != null && _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
                {
                    return _cachedToken;
                }
            }

            return await RequestNewTokenAsync();
        }


        // ==========================================================
        // ПРИНУДИТЕЛЬНОЕ обновление токена
        // ==========================================================

        public async Task<string> RefreshTokenAsync()
        {
            Console.WriteLine("OZON AUTH: принудительное обновление токена");

            lock (_lock)
            {
                _cachedToken = null;
                _tokenExpiry = DateTime.MinValue;
            }


            return await RequestNewTokenAsync();
        }


        // ==========================================================
        // Получение нового токена у Ozon
        // ==========================================================

        private async Task<string> RequestNewTokenAsync()
        {
            string url = _deliveryParam.auth_url;
            var requestParams = new
            {
                client_id = _deliveryParam.client_id,
                client_secret = _deliveryParam.client_secret,
                grant_type = "client_credentials",
                scope = new[]
                {
                    "delivery-api.all"
                }
            };

            int maxAttempts = 3;

            for (int i = 0; i < maxAttempts; i++)
            {
                var content = JsonContent.Create(requestParams);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                // ==============================================
                // testcookie / redirect
                // ==============================================

                if (response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.TemporaryRedirect)
                {
                    if (response.Headers.Location != null)
                    {
                        url = response.Headers.Location.IsAbsoluteUri
                                    ? response.Headers.Location.AbsoluteUri
                                    : new Uri(new Uri(url), response.Headers.Location).AbsoluteUri;
                        continue;
                    }
                }

                // ==============================================
                // Ошибка Ozon
                // ==============================================

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();

                    throw new Exception($"Сервер Ozon вернул статус " + $"{(int)response.StatusCode}. " + $"Ответ сервера: {errorResponse}");
                }

                // ==============================================
                // Читаем ответ
                // ==============================================

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;
                if (!root.TryGetProperty("access_token", out JsonElement tokenElement))
                {
                    throw new Exception("Параметр " + "'access_token' " + "не найден в ответе Ozon.");
                }

                string? token = tokenElement.GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new Exception("Ozon вернул пустой access_token.");
                }

                // ==============================================
                // Срок действия
                // ==============================================

                int expiresInSeconds = 3600;

                if (root.TryGetProperty("expires_in", out JsonElement expElement))
                {
                    if (expElement.ValueKind == JsonValueKind.Number)
                    {
                        expiresInSeconds = expElement.GetInt32();
                    }
                    else if (expElement.ValueKind == JsonValueKind.String)
                    {
                        if (
                            !int.TryParse(expElement.GetString(), out expiresInSeconds))
                        {
                            expiresInSeconds = 3600;
                        }
                    }
                }


                // ==============================================
                // Сохраняем токен
                // ==============================================

                lock (_lock)
                {
                    _cachedToken = token;
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresInSeconds);
                }

                Console.WriteLine($"OZON AUTH: новый токен получен. " + $"Срок: {expiresInSeconds} сек.");

                return token;
            }


            throw new Exception("Не удалось пройти проверку " + "testcookie: превышено количество " + "редиректов Ozon.");
        }
    }
}
