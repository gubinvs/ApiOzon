using Microsoft.AspNetCore.Mvc;
using ApiOzon.Core;
using ApiOzon.Models;
using System.Text.Json;

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentSberController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // Рекомендуется регистрировать HttpClient через AddHttpClient() в Program.cs
        public PaymentSberController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterPayment([FromBody] PaymentSberRequestDto request)
        {
            try
            {
                // 1. Берем креды из конфигурации (appsettings.json)
                var userName = _configuration["Sberbank:UserName"] ?? "ваш_логин-api";
                var password = _configuration["Sberbank:Password"] ?? "ваш_пароль";

                // Сбер принимает сумму строго в копейках (например, 150.50 руб -> 15050 копеек)
                long amountInKopecks = (long)Math.Round(request.Amount * 100);
                string orderNumber = $"INV-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

                // 2. Формируем тело запроса для Сбера
                var sberParams = new Dictionary<string, string>
                {
                    { "userName", userName },
                    { "password", password },
                    { "orderNumber", orderNumber },
                    { "amount", amountInKopecks.ToString() },
                    { "currency", "643" }, // Код рубля РФ
                    { "returnUrl", "https://вашсайт.ру/payment/success" },
                    { "failUrl", "https://вашсайт.ру/payment/fail" }
                };

                // Отправляем как x-www-form-urlencoded (базовый стандарт Сбера)
                var content = new FormUrlEncodedContent(sberParams);
                
                // Тестовый шлюз. Для продакшена смените на https://paygate.ru
                var sberUrl = "https://sberbank.ru";
                
                var response = await _httpClient.PostAsync(sberUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();
                
                var sberResult = JsonSerializer.Deserialize<SberRegisterResponse>(responseString);

                if (sberResult != null && !string.IsNullOrEmpty(sberResult.FormUrl))
                {
                    // ТУТ ВАЖНО: Сохрани sberResult.OrderId в свою БД к этому заказу!
                    // Он понадобится, чтобы проверить, дошли ли деньги, когда юзер вернется.

                    return Ok(new { formUrl = sberResult.FormUrl });
                }

                return BadRequest(new { message = sberResult?.ErrorMessage ?? "Ошибка при регистрации в Сбербанке" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Внутренняя ошибка сервера", details = ex.Message });
            }
        }
    }
}