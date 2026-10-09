using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using ApiOzon.Models;

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class OzonPayController (HttpClient httpClient, IConfiguration configuration) : ControllerBase
    {
        // Данные для авторизации (в продакшене перенести в Secret Manager / AppSettings)
        private readonly string _baseUrl = configuration["OzonPay:BaseUrl"] ?? "https://ozon.ru";
        private readonly string _clientId = configuration["OzonPay:ClientId"] ?? "YOUR_CLIENT_ID";
        private readonly string _apiKey = configuration["OzonPay:ApiKey"] ?? "YOUR_API_KEY";

        /// <summary>
        /// Инициализация платежа на стороне Ozon Pay
        /// </summary>
        // [HttpPost("create")]
        // public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentOzonRequest request)
        // {
        //     try
        //     {
        //         // Формируем тело запроса для Ozon Pay (переводим рубли в копейки)
        //         var ozonRequest = new OzonCreatePaymentRequest(
        //             OrderNumber: request.OrderId,
        //             AmountInKopecks: (int)(request.Amount * 100),
        //             Description: request.Description
        //         );

        //         // Настраиваем HTTP-запрос с заголовками авторизации
        //         var message = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}v1/payment/create");
        //         message.Headers.Add("Client-Id", _clientId);
        //         message.Headers.Add("Api-Key", _apiKey);
        //         message.Content = JsonContent.Create(ozonRequest);

        //         var response = await httpClient.SendAsync(message);

        //         if (!response.IsSuccessStatusCode)
        //         {
        //             var errorContent = await response.Content.ReadAsStringAsync();
        //             return StatusCode((int)response.StatusCode, $"Ошибка Ozon API: {errorContent}");
        //         }

        //         var ozonResponse = await response.Content.ReadFromJsonAsync<OzonPaymentResponse>();
                
        //         if (ozonResponse == null)
        //             return BadRequest("Не удалось прочитать ответ от Ozon Pay");

        //         // Возвращаем клиенту URL для оплаты
        //         return Ok(new { paymentUrl = ozonResponse.PaymentUrl, paymentId = ozonResponse.PaymentId });
        //     }
        //     catch (Exception ex)
        //     {
        //         return StatusCode(500, $"Внутренняя ошибка: {ex.Message}");
        //     }
        // }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentOzonRequest request)
        {
            // ИМИТАЦИЯ: вместо реального запроса к Ozon генерируем фейковый успешный ответ
            var mockPaymentId = Guid.NewGuid().ToString();
            
            // В тестовых целях перенаправляем на страницу-заглушку (например, ваш фронтенд)
            var mockPaymentUrl = $"http://localhost:5148?paymentId={mockPaymentId}&amount={request.Amount}";

            var ozonResponse = new OzonPaymentResponse(
                PaymentId: mockPaymentId,
                PaymentUrl: mockPaymentUrl,
                Status: "CREATED"
            );

            return Ok(new { paymentUrl = ozonResponse.PaymentUrl, paymentId = ozonResponse.PaymentId });
        }

        /// <summary>
        /// Точка приема Webhook уведомлений от Ozon Pay об изменении статуса
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook([FromBody] OzonWebhookPayload payload)
        {
            // ВАЖНО: Здесь должна быть проверка подписи запроса, если Ozon ее передает в заголовках, 
            // для предотвращения фейковых запросов.
            
            if (payload.Status == "SUCCEEDED")
            {
                // Логика при успешной оплате:
                // 1. Найти заказ в БД по payload.OrderNumber
                // 2. Проверить, совпадает ли сумма payload.Amount с суммой заказа в БД
                // 3. Сменить статус заказа на "Оплачен"
                // 4. Отгрузить товар или активировать услугу
                
                return Ok(); // Возвращаем 200 OK, чтобы Ozon понял, что уведомление доставлено успешно
            }
            
            if (payload.Status == "FAILED")
            {
                // Логика при отмене или ошибке платежа
                return Ok();
            }

            return Ok();
        }
    }
}
