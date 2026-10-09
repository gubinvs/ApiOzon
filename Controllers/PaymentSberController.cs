using ApiOzon.Core;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;
using ApiOzon.Models;

namespace ApiOzon.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class PaymentSberController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentSberController> _logger;

    public PaymentSberController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PaymentSberController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // ============================================================
    // POST /v1/PaymentSber
    //
    // Регистрация платежа в интернет-эквайринге Сбера.
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> RegisterPayment(
        [FromBody] PaymentSberRequestDto request)
    {
        if (request.Amount <= 0 ||
            request.Amount > 1_000_000_000)
        {
            return BadRequest(new
            {
                message = "Некорректная сумма платежа."
            });
        }

        var userName = _configuration["Sberbank:UserName"];
        var password = _configuration["Sberbank:Password"];
        var baseUrl = _configuration["Sberbank:BaseUrl"];

        var returnUrl = _configuration["Sberbank:ReturnUrl"]
            ?? "https://localhost:5001/payment/success";

        var failUrl = _configuration["Sberbank:FailUrl"]
            ?? "https://localhost:5001/payment/fail";

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogError(
                "Не настроены параметры интернет-эквайринга Сбера.");

            return StatusCode(500, new
            {
                message = "Ошибка конфигурации платёжного шлюза."
            });
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps &&
             baseUri.Scheme != Uri.UriSchemeHttp))
        {
            return StatusCode(500, new
            {
                message = "Некорректный адрес платёжного шлюза."
            });
        }

        try
        {
            // Сумма передаётся в копейках.
            var amountInKopecks = decimal.ToInt64(
                decimal.Round(
                    request.Amount * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

            // Уникальный номер заказа.
            var orderNumber =
                $"INV-{Guid.NewGuid():N}";

            var parameters = new Dictionary<string, string>
            {
                ["userName"] = userName,
                ["password"] = password,
                ["orderNumber"] = orderNumber,
                ["amount"] = amountInKopecks.ToString(
                    CultureInfo.InvariantCulture),
                ["currency"] = "643",
                ["returnUrl"] = returnUrl,
                ["failUrl"] = failUrl
            };

            using var content =
                new FormUrlEncodedContent(parameters);

            var client = _httpClientFactory.CreateClient(
                "SberbankClient");

            var requestUri = new Uri(
                baseUri,
                "register.do");

            using var response = await client.PostAsync(
                requestUri,
                content);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ошибка платёжного шлюза Сбера. HTTP {StatusCode}.",
                    (int)response.StatusCode);

                return StatusCode(502, new
                {
                    message =
                        "Платёжный шлюз временно недоступен."
                });
            }

            var result =
                JsonSerializer.Deserialize<SberRegisterResponse>(
                    responseBody,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (result == null ||
                string.IsNullOrWhiteSpace(result.FormUrl))
            {
                _logger.LogWarning(
                    "Сбер не зарегистрировал заказ {OrderNumber}. " +
                    "Код ошибки: {ErrorCode}; сообщение: {ErrorMessage}",
                    orderNumber,
                    result?.ErrorCode,
                    result?.ErrorMessage);

                return BadRequest(new
                {
                    message = result?.ErrorMessage
                        ?? "Не удалось зарегистрировать платёж."
                });
            }

            _logger.LogInformation(
                "Платёж зарегистрирован. Номер заказа: {OrderNumber}.",
                orderNumber);

            return Ok(new
            {
                orderNumber,
                orderId = result.OrderId,
                formUrl = result.FormUrl
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Ошибка соединения с платёжным шлюзом Сбера.");

            return StatusCode(502, new
            {
                message = "Не удалось связаться с банком."
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "Не удалось разобрать ответ платёжного шлюза Сбера.");

            return StatusCode(502, new
            {
                message = "Банк вернул некорректный ответ."
            });
        }
        catch (OverflowException ex)
        {
            _logger.LogWarning(
                ex,
                "Сумма платежа выходит за допустимый диапазон.");

            return BadRequest(new
            {
                message = "Сумма платежа слишком велика."
            });
        }
    }
}