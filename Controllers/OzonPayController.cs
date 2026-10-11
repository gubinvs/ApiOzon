using ApiOzon.Models;
using ApiOzon.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiOzon.Core;
using Microsoft.Extensions.Options;

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class OzonPayController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IOzonSignatureService _signatureService;
        private readonly  OzonPayParam _param;

        // Поля для авторизации (названия соответствуют вашему appsettings.json)
        private readonly string _baseUrl;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _notificationSecretKey;

        

        public OzonPayController (
            HttpClient httpClient, 
            IConfiguration configuration,
            IOzonSignatureService signatureService,
            IOptions<OzonPayParam>  options)
        {
            _httpClient = httpClient;
            _signatureService = signatureService;
            _param = options.Value;

            // Читаем ключи с именами из вашего конфига
            _baseUrl = _param.BaseUrl;
            _accessKey = _param.AccessKey;
            _secretKey = _param.SecretKey;
            _notificationSecretKey = _param.NotificationSecretKey;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentOzonRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var returnUrl = $"{Request.Scheme}://{Request.Host}/payment-result?order={request.OrderId}";

            var ozonRequest = new OzonCreatePaymentRequest(
                OrderNumber: request.OrderId,
                AmountInKopecks: (int)(request.Amount * 100),
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
                ),
                // Необязательные параметры можно опустить, если подходят значения по умолчанию
                Currency: "RUB", 
                Description: request.Description 
            );

            // Сериализуем JSON без лишних пробелов (Minified)
            var jsonPayload = JsonSerializer.Serialize(ozonRequest, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            });

            // Путь для подписи (без домена)
            var path = "v1/getOrderDetails";

            // Генерируем подпись: METHOD + PATH + BODY
            var signature = _signatureService.GenerateSignature("POST", path, jsonPayload, _secretKey);

            var message = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{path}");
            
            // Устанавливаем заголовки
            message.Headers.Add("X-Api-Key", _accessKey);
            message.Headers.Add("X-Signature", signature);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            message.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(message);
                var responseBody = await response.Content.ReadAsStringAsync();

               if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        statusCode = (int)response.StatusCode,
                        reasonPhrase = response.ReasonPhrase,
                        responseBody = responseBody,
                        requestUrl = $"{_baseUrl}{path}"
                    });
                }

                var ozonResponse = JsonSerializer.Deserialize<OzonPaymentResponse>(responseBody);
                if (ozonResponse == null) return BadRequest("Пустой ответ от Ozon");

                return Ok(new { paymentUrl = ozonResponse.PaymentUrl, paymentId = ozonResponse.PaymentId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка сети: {ex.Message}");
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            // 1. Читаем тело запроса как строку для проверки подписи
            var jsonBody = await new StreamReader(Request.Body).ReadToEndAsync();

            // 2. Получаем подпись из заголовка
            var incomingSignature = Request.Headers["X-Signature"].FirstOrDefault();

            // 3. Вычисляем ожидаемую подпись
            var expectedSignature = _signatureService.GenerateSignature("POST", "/payment/callback", jsonBody, _notificationSecretKey);

            // 4. Сравниваем (используем Ordinal для безопасности)
            if (string.IsNullOrEmpty(incomingSignature) || 
                !incomingSignature.Equals(expectedSignature, StringComparison.Ordinal))
            {
                return Unauthorized("Неверная подпись вебхука");
            }

            // 5. Если подпись верна, сбрасываем позицию потока и десериализуем модель
            Request.Body.Position = 0;
            var payload = await JsonSerializer.DeserializeAsync<OzonWebhookPayload>(Request.Body);

            if (payload == null) return BadRequest("Не удалось прочитать payload");

            // --- ВАША БИЗНЕС-ЛОГИКА (пример) ---
            // var order = await _dbContext.Orders.FindAsync(payload.OrderNumber);
            // if (order == null) return NotFound();

            // switch (payload.Status)
            // {
            //     case OzonPaymentStatus.SUCCEEDED:
            //         order.Status = OrderStatus.Paid;
            //         await _dbContext.SaveChangesAsync();
            //         break;
            //     case OzonPaymentStatus.FAILED:
            //         order.Status = OrderStatus.Failed;
            //         await _dbContext.SaveChangesAsync();
            //         break;
            // }

            // Ozon всегда должен получать 200 OK, иначе он будет слать запрос каждые 5 минут
            return Ok();
        }
    }
}