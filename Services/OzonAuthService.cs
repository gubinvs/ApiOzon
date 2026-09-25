using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ApiOzon.Services
{
    public interface IOzonAuthService
    {
        Task<string> GetTokenAsync();
    }

    public class OzonAuthService : IOzonAuthService
    {
        private readonly OzonDeliveryParam _deliveryParam;
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;

        // Кэширование токена прямо внутри Singleton-сервиса
        private string? _cachedToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly object _lock = new object();

        public OzonAuthService(IOptions<OzonDeliveryParam> options)
        {
            _deliveryParam = options.Value;
            _cookieContainer = new CookieContainer();

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false, // Отключаем авто-редирект для обработки testcookie
                CookieContainer = _cookieContainer // Куки сохраняются здесь на все время жизни приложения
            };

            _httpClient = new HttpClient(handler);
        }

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

            string url = _deliveryParam.auth_url;

            // Решение по умолчанию для Ozon Delivery: часто scope называется "delivery" или "api"
            // Если вы знаете точный scope из ЛК Ozon, замените пустую строку на него
            
            var requestParams = new 
            {
                client_id = _deliveryParam.client_id,
                client_secret = _deliveryParam.client_secret,
                grant_type = "client_credentials",
                scope = new[] { "delivery-api.all" } 
            };


            var content = JsonContent.Create(requestParams);
            int maxAttempts = 3;
            for (int i = 0; i < maxAttempts; i++)
            {
                
                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                // Обработка testcookie
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

                // === ВАЖНЫЙ БЛОК: Логируем подробный текст ошибки 400 ===
                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Сервер Ozon вернул статус {(int)response.StatusCode}. Ответ сервера: {errorResponse}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    JsonElement root = doc.RootElement;
                    
                    if (root.TryGetProperty("access_token", out JsonElement tokenElement))
                    {
                        _cachedToken = tokenElement.GetString();

                        // Безопасное чтение expires_in (обрабатываем и число, и строку)
                        int expiresInSeconds = 3600; // значение по умолчанию (1 час)

                        if (root.TryGetProperty("expires_in", out JsonElement expElement))
                        {
                            if (expElement.ValueKind == JsonValueKind.Number)
                            {
                                expiresInSeconds = expElement.GetInt32();
                            }
                            else if (expElement.ValueKind == JsonValueKind.String)
                            {
                                // Если Ozon прислал "3600" как строку, конвертируем её в int
                                if (!int.TryParse(expElement.GetString(), out expiresInSeconds))
                                {
                                    expiresInSeconds = 3600; 
                                }
                            }
                        }

                        // Обновляем время жизни кэша токена
                        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresInSeconds);

                        return _cachedToken!;
                    }
                    
                    throw new Exception("Параметр 'access_token' не найден в ответе Ozon.");
                }
            }

            throw new Exception("Не удалось пройти проверку testcookie: превышено количество редиректов Ozon.");
        }

    }
}
