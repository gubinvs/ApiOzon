using ApiOzon.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading.Tasks; // Для корректной работы async/await

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class OzonPayController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _clientId;
        private readonly string _apiKey;
        
        // Добавлено: задержка для эмуляции сети
        private static readonly TimeSpan _networkLatency = TimeSpan.FromMilliseconds(800);

        public OzonPayController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _baseUrl = configuration["OzonPay:BaseUrl"] ?? "https://api.ozon.ru";
            _clientId = configuration["OzonPay:ClientId"] ?? "YOUR_CLIENT_ID";
            _apiKey = configuration["OzonPay:ApiKey"] ?? "YOUR_API_KEY";
        }

        /// <summary>
        /// Инициализация платежа. 
        /// Переключатель между реальным API и тестовым стендом.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentOzonRequest request)
        {
            // Валидация входных данных
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Проверка переменной конфигурации (в appsettings.json: "OzonPay:UseMock": true)
            bool useMock = _configuration.GetValue<bool>("OzonPay:UseMock");

            if (useMock)
            {
                return await CreatePaymentMock(request);
            }
            else
            {
                return await CreatePaymentReal(request);
            }
        }

        /// <summary>
        /// Тестовый режим: имитирует ответ Ozon без внешних запросов.
        /// </summary>
        private async Task<IActionResult> CreatePaymentMock(CreatePaymentOzonRequest request)
        {
            await Task.Delay(_networkLatency); // Имитация сетевого лага

            var mockPaymentId = Guid.NewGuid().ToString();
            var mockPaymentUrl = $"http://localhost:5148/pay?paymentId={mockPaymentId}";

            var response = new OzonPaymentResponse(
                PaymentId: mockPaymentId,
                PaymentUrl: mockPaymentUrl,
                Status: OzonPaymentStatus.CREATED
            );

            return Ok(new { paymentUrl = response.PaymentUrl, paymentId = response.PaymentId });
        }

        /// <summary>
        /// Продакшен режим: реальный запрос к API Ozon Pay.
        /// </summary>
        private async Task<IActionResult> CreatePaymentReal(CreatePaymentOzonRequest request)
        {
            try
            {
                // Формируем тело запроса для Ozon (переводим рубли в копейки)
                var ozonRequest = new OzonCreatePaymentRequest(
                    OrderNumber: request.OrderId,
                    AmountInKopecks: (int)(request.Amount * 100),
                    Description: request.Description
                );

                // Настраиваем HTTP-запрос
                var message = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/payment/create");
                message.Headers.Add("Client-Id", _clientId);
                message.Headers.Add("Api-Key", _apiKey);
                message.Content = JsonContent.Create(ozonRequest);

                // Отправляем запрос
                var response = await _httpClient.SendAsync(message);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, $"Ошибка Ozon API: {errorContent}");
                }

                var ozonResponse = await response.Content.ReadFromJsonAsync<OzonPaymentResponse>();
                
                if (ozonResponse == null)
                    return BadRequest("Не удалось прочитать ответ от Ozon Pay");

                // Возвращаем клиенту URL для оплаты
                return Ok(new { paymentUrl = ozonResponse.PaymentUrl, paymentId = ozonResponse.PaymentId });
            }
            catch (Exception ex)
            {
                // Логирование ex (ILogger) рекомендуется добавить здесь
                return StatusCode(500, $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        /// <summary>
        /// Точка приема Webhook уведомлений от Ozon Pay.
        /// ВАЖНО: В продакшене необходимо проверять подпись (X-Ozon-Signature).
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] OzonWebhookPayload payload)
        {
            await Task.Delay(_networkLatency);

            // 1. Проверка подписи (заглушка)
            // if (!ValidateSignature(Request.Headers, payload)) return Unauthorized();

            // 2. Поиск заказа в БД по payload.OrderNumber
            // var order = await _dbContext.Orders.FindAsync(payload.OrderNumber);
            // if (order == null) return NotFound();

            // 3. Проверка суммы (защита от подмены данных)
            // if (payload.AmountInRubles != order.TotalAmount) 
            //     return BadRequest("Сумма в вебхуке не совпадает с суммой заказа");

            // 4. Смена статуса заказа
            switch (payload.Status)
            {
                case OzonPaymentStatus.SUCCEEDED:
                    // order.Status = OrderStatus.Paid;
                    // Отправить флаг, который позволит продолжить оформление доставки
                    break;

                case OzonPaymentStatus.FAILED:
                case OzonPaymentStatus.CANCELLED:
                    // order.Status = OrderStatus.PaymentFailed;
                    break;
            }

            // await _dbContext.SaveChangesAsync();

            // Ozon ожидает 200 OK, иначе будет слать вебхук повторно по экспоненте
            return Ok();
        }
    }
}