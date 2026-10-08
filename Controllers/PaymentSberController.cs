using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiOzon.Core;
using ApiOzon.Models; // Убедитесь, что этот namespace верный для вашего PaymentSberRequestDto

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class PaymentSberController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentSberController> _logger;

        public PaymentSberController(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            ILogger<PaymentSberController> logger)
        {
            // Берем настроенный клиент. Не забывайте добавить слеш в конце BaseAddress в Program.cs!
            _httpClient = httpClientFactory.CreateClient("SberBusinessClient");
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost] // Добавили явный роут для соответствия фронтенду (/v1/PaymentSber/register)
        public async Task<IActionResult> RegisterPayment([FromBody] PaymentSberRequestDto request)
        {
            _logger.LogInformation("=== Начало регистрации платежа Sber Business API. Сумма: {Amount} ===", request.Amount);

            try
            {
                // 1. ПОЛУЧЕНИЕ ТОКЕНА (OAuth2)
                _logger.LogInformation("Отправка запроса на получение OAuth-токена в песочницу Сбера...");
                
                // ИСПРАВЛЕНО: Убран лидирующий слэш "/", чтобы сохранить порт :9443 из Program.cs
                // Если так работает, то 100% проблема в настройке HttpClientFactory в Program.cs
                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://fintech-test.sberbank.ru:9443");

                
                var tokenParams = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", "92418" }, 
                    { "scope", "openid SCOPE_TEST_590302376610_5c90338c-9139-4fdf-80f5-0b757f5bc812" } 
                };
                
                tokenRequest.Content = new FormUrlEncodedContent(tokenParams);
                
                var tokenResponse = await _httpClient.SendAsync(tokenRequest);
                var tokenRaw = await tokenResponse.Content.ReadAsStringAsync();
                
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Сбер отказал в выдаче токена. Код: {StatusCode}. Ответ: {Response}", 
                        tokenResponse.StatusCode, tokenRaw);
                        
                    return StatusCode((int)tokenResponse.StatusCode, new { message = "Ошибка авторизации в Sber API", details = tokenRaw });
                }

                var tokenData = JsonSerializer.Deserialize<SberTokenResponse>(tokenRaw);
                
                // ИСПРАВЛЕНО: Защита от Nullable-ошибки. Проверяем токен на null перед использованием
                if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
                {
                    _logger.LogError("JSON десериализовался, но AccessToken пуст или отсутствует. Сырой ответ: {Raw}", tokenRaw);
                    return BadRequest(new { message = "Шлюз вернул пустой или некорректный токен" });
                }

                string accessToken = tokenData.AccessToken;
                _logger.LogInformation("OAuth-токен успешно получен. Длина: {Length} симв.", accessToken.Length);

                // 2. ВЫСТАВЛЕНИЕ СЧЕТА (PAY_DOC_RU_INVOICE)
                _logger.LogInformation("Формирование и отправка инвойса (документа оплаты)...");
                
                // ИСПРАВЛЕНО: Убран лидирующий слэш "/"
                var invoiceRequest = new HttpRequestMessage(HttpMethod.Post, "v1/payments"); 
                invoiceRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                var invoiceBody = new
                {
                    amount = request.Amount, // decimal отправляется в JSON как число (например, 25000)
                    currency = "RUB",
                    redirectUri = "https://ec-market.ru"
                };

                string jsonBody = JsonSerializer.Serialize(invoiceBody);
                _logger.LogInformation("Тело запроса инвойса: {Json}", jsonBody);

                invoiceRequest.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var invoiceResponse = await _httpClient.SendAsync(invoiceRequest);
                var invoiceRaw = await invoiceResponse.Content.ReadAsStringAsync();

                if (invoiceResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Счет в песочнице успешно зарегистрирован. Ответ Сбера: {Response}", invoiceRaw);
                    
                    var deserializedResponse = JsonSerializer.Deserialize<object>(invoiceRaw);
                    return Ok(deserializedResponse);
                }

                _logger.LogWarning("Сбер вернул ошибку при создании инвойса. Код: {StatusCode}. Ответ: {Response}", 
                    invoiceResponse.StatusCode, invoiceRaw);

                return BadRequest(new { message = "Ошибка выставления счета в Сбере", details = invoiceRaw });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическое исключение внутри контроллера: {Message}", ex.Message);
                return StatusCode(500, new { message = "Внутренняя ошибка бэкенда", details = ex.Message });
            }
        }
    }

    public class SberTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
