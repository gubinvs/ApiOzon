using Microsoft.AspNetCore.Mvc;
using ApiOzon.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ApiOzon.Core;

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
            IHttpClientFactory httpClientFactory, // Внедряем фабрику вместо прямого HttpClient
            IConfiguration configuration, 
            ILogger<PaymentSberController> logger)
        {
            // Извлекаем именно тот клиент, который умеет работать с папкой Certificates
            _httpClient = httpClientFactory.CreateClient("SberbankClient");
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> RegisterPayment([FromBody] PaymentSberRequestDto request)
        {
            try
            {
                // 1. Извлекаем конфигурацию
                var userName = _configuration["Sberbank:UserName"];
                var password = _configuration["Sberbank:Password"];
                var baseUrl = _configuration["Sberbank:BaseUrl"];
                var returnUrl = _configuration["Sberbank:ReturnUrl"] ?? "https://localhost:5001/payment/success";
                var failUrl = _configuration["Sberbank:FailUrl"] ?? "https://localhost:5001/payment/fail";

                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(baseUrl))
                {
                    _logger.LogError("Критические настройки Сбербанка отсутствуют в конфигурации.");
                    return StatusCode(500, new { message = "Ошибка конфигурации платежного шлюза" });
                }

                // Сумма в копейках с округлением
                long amountInKopecks = (long)Math.Round(request.Amount * 100);
                string orderNumber = $"INV-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

                // 2. Формируем x-www-form-urlencoded параметры
                var sberParams = new Dictionary<string, string>
                {
                    { "userName", userName },
                    { "password", password },
                    { "orderNumber", orderNumber },
                    { "amount", amountInKopecks.ToString() },
                    { "currency", "643" }, 
                    { "returnUrl", returnUrl },
                    { "failUrl", failUrl }
                };

                using var content = new FormUrlEncodedContent(sberParams);
                
                // Полный URL метода регистрации заказа в Сбере (обычно это register.do)
               var requestUrl = $"{baseUrl.TrimEnd('/')}/register.do";  
                
                // 3. Выполняем запрос
                var response = await _httpClient.PostAsync(requestUrl, content);
                
                // Проверяем сетевую успешность (200 OK)
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Сбербанк вернул сетевую ошибку: {StatusCode}", response.StatusCode);
                    return StatusCode(502, new { message = "Платежный шлюз временно недоступен" });
                }

                var responseString = await response.Content.ReadAsStringAsync();
                
                // Обязательно настраиваем CamelCase для десериализации ответа Сбера
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var sberResult = JsonSerializer.Deserialize<SberRegisterResponse>(responseString, deserializeOptions);

                // 4. Анализируем бизнес-логику ответа
                if (sberResult != null && !string.IsNullOrEmpty(sberResult.FormUrl))
                {
                    // TODO: Сохраните sberResult.OrderId и orderNumber в вашу БД к текущему заказу!
                    _logger.LogInformation("Заказ {OrderNumber} успешно зарегистрирован в Сбере. OrderId: {SberOrderId}", orderNumber, sberResult.OrderId);

                    return Ok(new { formUrl = sberResult.FormUrl, orderId = sberResult.OrderId });
                }

                // Обработка бизнес-ошибки Сбера (например, неверные креды или дубликат orderNumber)
                _logger.LogWarning("Ошибка регистрации платежа Сбера: {ErrCode} - {ErrMessage}", sberResult?.ErrorCode, sberResult?.ErrorMessage);
                return BadRequest(new { message = sberResult?.ErrorMessage ?? "Ошибка при регистрации платежа в банке" });
            }
            catch (Exception ex)
            {
                // Логируем исключение, не раскрывая стек вызовов клиенту
                _logger.LogError(ex, "Критическая ошибка при регистрации платежа Сбербанка");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера при обработке платежа" });
            }
        }
    }
}
