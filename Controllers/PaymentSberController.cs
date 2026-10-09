using Microsoft.AspNetCore.Mvc;
<<<<<<< HEAD
using ApiOzon.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;
=======
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
>>>>>>> 6dca8e601affc9cfa6a0738a4e4baecb4032d9db
using ApiOzon.Core;

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class PaymentSberController : ControllerBase
    {
<<<<<<< HEAD
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
=======
        private const string AccessTokenCacheKey = "SberAccessToken";
        private const string RefreshTokenCacheKey = "SberRefreshToken";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentSberController> _logger;
        private readonly IMemoryCache _memoryCache;

        public PaymentSberController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<PaymentSberController> logger,
            IMemoryCache memoryCache)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        // ============================================================
        // GET /v1/PaymentSber/authorize
        //
        // Начало OAuth авторизации в Sber Business API.
        // ============================================================

        [HttpGet("authorize")]
        public IActionResult Authorize()
        {
            try
            {
                var clientId =
                    _configuration["Sberbank:ClientId"]
                    ?? _configuration["Sberbank:UserName"];

                var redirectUri =
                    _configuration["Sberbank:RedirectUri"];

                var scope =
                    _configuration["Sberbank:ScopeV1"];

                if (string.IsNullOrWhiteSpace(clientId))
                {
                    return BadRequest(new
                    {
                        message = "Не задан Sberbank:ClientId"
                    });
                }

                if (string.IsNullOrWhiteSpace(redirectUri))
                {
                    return BadRequest(new
                    {
                        message = "Не задан Sberbank:RedirectUri"
                    });
                }

                if (string.IsNullOrWhiteSpace(scope))
                {
                    return BadRequest(new
                    {
                        message = "Не задан Sberbank:ScopeV1"
                    });
                }

                // ----------------------------------------------------
                // state
                // ----------------------------------------------------

                var state = Guid.NewGuid().ToString("N");

                // ----------------------------------------------------
                // nonce
                // ----------------------------------------------------

                var nonce = Guid.NewGuid().ToString("N");

                // ----------------------------------------------------
                // Сохраняем state в cookie.
                // ----------------------------------------------------

                Response.Cookies.Append(
                    "SberOAuthState",
                    state,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        MaxAge = TimeSpan.FromMinutes(5)
                    });

                // ----------------------------------------------------
                // Формируем URL авторизации Sber.
                // ----------------------------------------------------

                var authorizationUrl =
                    "https://sbi.sberbank.ru:9443/ic/sso/api/v2/oauth/authorize"
                    + "?prompt=login"
                    + "&redirect_uri="
                    + Uri.EscapeDataString(redirectUri)
                    + "&nonce="
                    + Uri.EscapeDataString(nonce)
                    + "&state="
                    + Uri.EscapeDataString(state)
                    + "&scope="
                    + Uri.EscapeDataString(scope)
                    + "&response_type=code"
                    + "&client_id="
                    + Uri.EscapeDataString(clientId);

                _logger.LogInformation(
                    "Переход на авторизацию Sber. ClientId: {ClientId}",
                    clientId);

                return Redirect(authorizationUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ошибка формирования URL авторизации Sber");

                return StatusCode(500, new
                {
                    message = "Ошибка формирования авторизации Sber",
                    details = ex.Message
                });
            }
        }


        // ============================================================
        // GET /v1/PaymentSber/callback
        //
        // Sber возвращает пользователя сюда:
        //
        // /callback?code=...&state=...
        //
        // ============================================================

        [HttpGet("callback")]
        public async Task<IActionResult> Callback(
            [FromQuery] string? code,
            [FromQuery] string? state)
        {
            _logger.LogInformation(
                "=== Получен callback от Sber ===");

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new
                {
                    message = "Sber не передал authorization code"
                });
            }

            // --------------------------------------------------------
            // Проверяем state.
            // --------------------------------------------------------

            var savedState =
                Request.Cookies["SberOAuthState"];

            if (!string.IsNullOrWhiteSpace(savedState) &&
                !string.Equals(
                    savedState,
                    state,
                    StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Sber OAuth state не совпадает.");

                return BadRequest(new
                {
                    message = "Некорректный OAuth state"
                });
            }

            // --------------------------------------------------------
            // Обмениваем authorization code на токены.
            // --------------------------------------------------------

            return await ExchangeAuthorizationCode(code);
        }


        // ============================================================
        // POST /v1/PaymentSber/token
        //
        // Ручной вариант обмена code на token.
        //
        // Body:
        //
        // {
        //   "code": "..."
        // }
        //
        // ============================================================

        [HttpPost("token")]
        public async Task<IActionResult> Token(
            [FromBody] SberAuthorizationCodeRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new
                {
                    message = "Authorization code не передан"
                });
            }

            return await ExchangeAuthorizationCode(
                request.Code,
                request.State);
        }


        // ============================================================
        // ОБМЕН AUTHORIZATION CODE НА TOKEN
        // ============================================================

        private async Task<IActionResult> ExchangeAuthorizationCode(
            string code,
            string? state = null)
        {
            try
            {
                var clientId =
                    _configuration["Sberbank:ClientId"]
                    ?? _configuration["Sberbank:UserName"];

                var clientSecret =
                    _configuration["Sberbank:ClientSecret"];

                var redirectUri =
                    _configuration["Sberbank:RedirectUri"];

                if (string.IsNullOrWhiteSpace(clientId))
                {
                    return BadRequest(new
                    {
                        message =
                            "Не задан Sberbank:ClientId"
                    });
                }

                if (string.IsNullOrWhiteSpace(clientSecret))
                {
                    return BadRequest(new
                    {
                        message =
                            "Не задан Sberbank:ClientSecret"
                    });
                }

                if (string.IsNullOrWhiteSpace(redirectUri))
                {
                    return BadRequest(new
                    {
                        message =
                            "Не задан Sberbank:RedirectUri"
                    });
                }

                // ----------------------------------------------------
                // SberBusinessClient уже содержит:
                //
                // - клиентский .p12
                // - TLS
                // - проверку сертификата Sber
                //
                // ----------------------------------------------------

                var httpClient =
                    _httpClientFactory.CreateClient(
                        "SberBusinessClient");

                var tokenRequest =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        "ic/sso/api/v2/oauth/token");

                // ----------------------------------------------------
                // OAuth authorization_code
                // ----------------------------------------------------

                tokenRequest.Content =
                    new FormUrlEncodedContent(
                        new Dictionary<string, string>
                        {
                            ["grant_type"] =
                                "authorization_code",

                            ["code"] =
                                code,

                            ["client_id"] =
                                clientId,

                            ["client_secret"] =
                                clientSecret,

                            ["redirect_uri"] =
                                redirectUri
                        });

                _logger.LogInformation(
                    "Отправляем authorization code в Sber OAuth. ClientId: {ClientId}",
                    clientId);

                var response =
                    await httpClient.SendAsync(tokenRequest);

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "Ответ Sber OAuth: HTTP {StatusCode}",
                    (int)response.StatusCode);

                // ----------------------------------------------------
                // Ошибка Sber.
                // ----------------------------------------------------

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Sber не выдал токен. HTTP {StatusCode}. Ответ: {Response}",
                        response.StatusCode,
                        responseBody);

                    return StatusCode(
                        (int)response.StatusCode,
                        new
                        {
                            message =
                                "Sber API не выдал access token",

                            statusCode =
                                (int)response.StatusCode,

                            details =
                                responseBody
                        });
                }

                // ----------------------------------------------------
                // Разбираем JSON.
                // ----------------------------------------------------

                var tokenData =
                    JsonSerializer.Deserialize<SberTokenResponse>(
                        responseBody);

                if (tokenData == null ||
                    string.IsNullOrWhiteSpace(
                        tokenData.AccessToken))
                {
                    _logger.LogError(
                        "Sber вернул ответ без access_token. Ответ: {Response}",
                        responseBody);

                    return BadRequest(new
                    {
                        message =
                            "Sber вернул некорректный ответ",

                        details =
                            responseBody
                    });
                }

                // ----------------------------------------------------
                // Сохраняем AccessToken.
                //
                // Сам токен в лог НЕ выводим.
                // ----------------------------------------------------

                var accessLifetime =
                    tokenData.ExpiresIn > 0
                        ? TimeSpan.FromSeconds(
                            tokenData.ExpiresIn)
                        : TimeSpan.FromMinutes(60);

                _memoryCache.Set(
                    AccessTokenCacheKey,
                    tokenData.AccessToken,
                    accessLifetime);

                // ----------------------------------------------------
                // RefreshToken.
                // ----------------------------------------------------

                if (!string.IsNullOrWhiteSpace(
                    tokenData.RefreshToken))
                {
                    _memoryCache.Set(
                        RefreshTokenCacheKey,
                        tokenData.RefreshToken);
                }

                _logger.LogInformation(
                    "=== Access Token Sber успешно получен ===");

                // ----------------------------------------------------
                // Возвращаем информацию без необходимости
                // передавать access_token обратно в браузер.
                // ----------------------------------------------------

                return Ok(new
                {
                    success = true,
                    token_type = tokenData.TokenType,
                    expires_in = tokenData.ExpiresIn,
                    scope = tokenData.Scope,
                    message =
                        "Авторизация Sber API успешно выполнена"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Критическая ошибка получения токена Sber");

                return StatusCode(500, new
                {
                    message =
                        "Внутренняя ошибка OAuth Sber",

                    details =
                        ex.Message
                });
            }
        }


        // ============================================================
        // POST /v1/PaymentSber/refresh
        //
        // Обновление AccessToken через RefreshToken.
        // ============================================================

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                if (!_memoryCache.TryGetValue(
                        RefreshTokenCacheKey,
                        out string? refreshToken) ||
                    string.IsNullOrWhiteSpace(refreshToken))
                {
                    return BadRequest(new
                    {
                        message =
                            "RefreshToken отсутствует. " +
                            "Необходимо выполнить авторизацию заново."
                    });
                }

                var clientId =
                    _configuration["Sberbank:ClientId"]
                    ?? _configuration["Sberbank:UserName"];

                var clientSecret =
                    _configuration["Sberbank:ClientSecret"];

                if (string.IsNullOrWhiteSpace(clientId))
                {
                    return BadRequest(new
                    {
                        message =
                            "Не задан Sberbank:ClientId"
                    });
                }

                if (string.IsNullOrWhiteSpace(clientSecret))
                {
                    return BadRequest(new
                    {
                        message =
                            "Не задан Sberbank:ClientSecret"
                    });
                }

                var httpClient =
                    _httpClientFactory.CreateClient(
                        "SberBusinessClient");

                var tokenRequest =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        "ic/sso/api/v2/oauth/token");

                tokenRequest.Content =
                    new FormUrlEncodedContent(
                        new Dictionary<string, string>
                        {
                            ["grant_type"] =
                                "refresh_token",

                            ["refresh_token"] =
                                refreshToken,

                            ["client_id"] =
                                clientId,

                            ["client_secret"] =
                                clientSecret
                        });

                _logger.LogInformation(
                    "Запрос обновления Sber AccessToken.");

                var response =
                    await httpClient.SendAsync(tokenRequest);

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Sber не обновил токен. HTTP {StatusCode}. Ответ: {Response}",
                        response.StatusCode,
                        responseBody);

                    return StatusCode(
                        (int)response.StatusCode,
                        new
                        {
                            message =
                                "Sber API не обновил токен",

                            statusCode =
                                (int)response.StatusCode,

                            details =
                                responseBody
                        });
                }

                var tokenData =
                    JsonSerializer.Deserialize<SberTokenResponse>(
                        responseBody);

                if (tokenData == null ||
                    string.IsNullOrWhiteSpace(
                        tokenData.AccessToken))
                {
                    return BadRequest(new
                    {
                        message =
                            "Sber вернул некорректный ответ",

                        details =
                            responseBody
                    });
                }

                var accessLifetime =
                    tokenData.ExpiresIn > 0
                        ? TimeSpan.FromSeconds(
                            tokenData.ExpiresIn)
                        : TimeSpan.FromMinutes(60);

                _memoryCache.Set(
                    AccessTokenCacheKey,
                    tokenData.AccessToken,
                    accessLifetime);

                if (!string.IsNullOrWhiteSpace(
                    tokenData.RefreshToken))
                {
                    _memoryCache.Set(
                        RefreshTokenCacheKey,
                        tokenData.RefreshToken);
                }

                _logger.LogInformation(
                    "AccessToken Sber успешно обновлен.");

                return Ok(new
                {
                    success = true,
                    token_type = tokenData.TokenType,
                    expires_in = tokenData.ExpiresIn,
                    scope = tokenData.Scope,
                    message =
                        "AccessToken успешно обновлен"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Ошибка обновления AccessToken Sber");

                return StatusCode(500, new
                {
                    message =
                        "Внутренняя ошибка обновления токена",

                    details =
                        ex.Message
                });
            }
        }


        // ============================================================
        // POST /v1/PaymentSber
        //
        // Body:
        //
        // {
        //     "amount": 2550.00
        // }
        //
        // AccessToken берется СЕРВЕРОМ из IMemoryCache.
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> RegisterPayment(
            [FromBody] PaymentSberRequestDto request)
        {
            _logger.LogInformation(
                "=== Начало регистрации платежа Sber. Сумма: {Amount} ===",
                request.Amount);

            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "Сумма платежа должна быть больше нуля"
                    });
                }

                // ----------------------------------------------------
                // Получаем AccessToken из памяти сервера.
                // ----------------------------------------------------

                if (!_memoryCache.TryGetValue(
                        AccessTokenCacheKey,
                        out string? accessToken) ||
                    string.IsNullOrWhiteSpace(accessToken))
                {
                    return Unauthorized(new
                    {
                        message =
                            "Нет действующего AccessToken Sber. " +
                            "Сначала выполните /v1/PaymentSber/authorize"
                    });
                }

                var httpClient =
                    _httpClientFactory.CreateClient(
                        "SberBusinessClient");

                // ----------------------------------------------------
                // Создание платежа.
                // ----------------------------------------------------

                var paymentRequest =
                    new HttpRequestMessage(
                        HttpMethod.Post,
                        "v1/payments");

                paymentRequest.Headers.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        accessToken);

                var redirectUri =
                    _configuration[
                        "Sberbank:RedirectUri"];

                var paymentBody = new
                {
                    amount = request.Amount,
                    currency = "RUB",
                    redirectUri = redirectUri
                };

                var jsonBody =
                    JsonSerializer.Serialize(
                        paymentBody);

                paymentRequest.Content =
                    new StringContent(
                        jsonBody,
                        Encoding.UTF8,
                        "application/json");

                _logger.LogInformation(
                    "Отправка платежа в Sber API. Сумма: {Amount}",
                    request.Amount);

                var response =
                    await httpClient.SendAsync(
                        paymentRequest);

                var responseBody =
                    await response.Content.ReadAsStringAsync();

                _logger.LogInformation(
                    "Ответ Sber при создании платежа: HTTP {StatusCode}",
                    (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Sber отказал в создании платежа. " +
                        "HTTP {StatusCode}. Ответ: {Response}",
                        response.StatusCode,
                        responseBody);

                    return StatusCode(
                        (int)response.StatusCode,
                        new
                        {
                            message =
                                "Ошибка создания платежа в Sber",

                            statusCode =
                                (int)response.StatusCode,

                            details =
                                responseBody
                        });
                }

                _logger.LogInformation(
                    "=== Платеж успешно зарегистрирован в Sber ===");

                try
                {
                    var result =
                        JsonSerializer.Deserialize<object>(
                            responseBody);

                    return Ok(result);
                }
                catch
                {
                    return Ok(new
                    {
                        response = responseBody
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Критическая ошибка регистрации платежа Sber");

                return StatusCode(500, new
                {
                    message =
                        "Внутренняя ошибка регистрации платежа",

                    details =
                        ex.Message
                });
            }
        }
    }


    // ================================================================
    // DTO authorization code
    // ================================================================

    public class SberAuthorizationCodeRequest
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }
    }


    // ================================================================
    // Ответ OAuth Sber
    // ================================================================

    public class SberTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("id_token")]
        public string? IdToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}
>>>>>>> 6dca8e601affc9cfa6a0738a4e4baecb4032d9db
