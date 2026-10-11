using ApiOzon.Models;
using ApiOzon.Core;
using ApiOzon.Services;
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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var returnUrl = $"{Request.Scheme}://{Request.Host}/payment-result?order={request.OrderId}";

            var ozonRequest = new OzonCreatePaymentRequest(
                OrderNumber: request.OrderId,
                AmountInKopecks: (int)(request.Amount * 100),
                Description: request.Description,
                CustomerEmail: request.CustomerEmail,
                ReturnUrl: returnUrl,
                Receipt: new OzonReceipt(
                    CustomerEmail: request.CustomerEmail,
                    Items: new List<OzonReceiptItem>
                    {
                        new(
                            Description: request.Description ?? "Оплата заказа",
                            Quantity: 1,
                            AmountInKopecks: (int)(request.Amount * 100),
                            VatCode: 2
                        )
                    }
                )
            );

            var jsonPayload = JsonSerializer.Serialize(ozonRequest, new JsonSerializerOptions
            {
                // Ozon Acquiring чувствителен к пробелам и порядку, используем Default
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
            });

            // Определяем путь для подписи (без BaseUrl!)
            var path = "/v1/payment/create";

            // Генерируем подпись по правилам Acquiring
            var signature = _signatureService.GenerateSignature("POST", path, jsonPayload, _secretKey);

            var message = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{path}");
            
            // Заголовки
            message.Headers.Add("X-Api-Key", _accessKey);
            message.Headers.Add("X-Signature", signature);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            message.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(message);
                
                // Читаем ответ ДО проверки статуса, чтобы увидеть тело ошибки
                var responseBody = await response.Content.ReadAsStringAsync(); 

                if (!response.IsSuccessStatusCode)
                {
                    // В лог выведите: path, jsonPayload, signature, responseBody
                    return StatusCode((int)response.StatusCode, $"Ошибка Ozon API: {responseBody}");
                }

                var ozonResponse = JsonSerializer.Deserialize<OzonPaymentResponse>(responseBody);
                return Ok(new { paymentUrl = ozonResponse?.PaymentUrl, paymentId = ozonResponse?.PaymentId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка сети: {ex.Message}");
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