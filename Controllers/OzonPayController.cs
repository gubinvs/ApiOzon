using ApiOzon.Models;
using ApiOzon.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiOzon.Core;
using Microsoft.Extensions.Options;
using System.IO.Compression; 

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class OzonPayController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IOzonSignatureService _signatureService;
        private readonly OzonPayParam _param;

        public OzonPayController(
            HttpClient httpClient, 
            IOzonSignatureService signatureService,
            IOptions<OzonPayParam> options)
        {
            _httpClient = httpClient;
            _signatureService = signatureService;
            _param = options.Value;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentOzonRequest clientRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 1. Формируем запрос на основе параметров из конфигурации
                var finalizedRequest = clientRequest with
                {
                    AccessKey = _param.AccessKey,
                    FailUrl = _param.FailUrl,
                    SuccessUrl = _param.SuccessUrl,
                    NotificationUrl = string.Empty, 
                    ReceiptEmail = _param.ReceiptEmail,
                    
                    Mode = "MODE_FULL",
                    PaymentAlgorithm = "PAY_ALGO_SMS",      
                    FiscalizationType = "FISCAL_TYPE_SINGLE", 
                    EnableFiscalization = true,

                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
                    Amount = clientRequest.Amount with { CurrencyCode = "643" },
                    DeliverySettings = new DeliverySettings { IsEnabled = true },
                    Items = UpdateItemsCurrency(clientRequest.Items)
                };

                var jsonOptions = new JsonSerializerOptions 
                { 
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull 
                };

                // 2. Генерируем "чистый" JSON для расчета сигнатуры подписи
                string jsonForSignature = JsonSerializer.Serialize(finalizedRequest with { RequestSign = string.Empty }, jsonOptions);

                string httpMethod = "POST";
                string path = "/v1/createOrder"; 

                // 3. Вычисляем подпись через твой сервис
                string signature = _signatureService.GenerateSignature(httpMethod, path, jsonForSignature, _param.SecretKey);
                
                // 4. Вшиваем готовую подпись в финальный JSON-пакет
                finalizedRequest = finalizedRequest with { RequestSign = signature };
                string requestBodyJson = JsonSerializer.Serialize(finalizedRequest, jsonOptions);

                // 5. Отправляем запрос на payapi.ozon.ru
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_param.BaseUrl.TrimEnd('/')}{path}")
                {
                    Content = new StringContent(requestBodyJson, Encoding.UTF8, "application/json")
                };

                httpRequest.Headers.Clear();
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpRequest.Headers.Add("Client-Id", _param.AccessKey); 

                // МЕНЯЕМ ТУТ: Убираем из запроса "br", оставляем только gzip и deflate.
                // Это заставит прокси Ozon сжать ответ в Gzip, который .NET гарантированно раскусит.
                httpRequest.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                httpRequest.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
                httpRequest.Headers.TryAddWithoutValidation("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8");
                httpRequest.Headers.TryAddWithoutValidation("Connection", "keep-alive");

                var httpResponse = await _httpClient.SendAsync(httpRequest);

                // 6. Обработка ошибок шлюза с полной декомпрессией
                if (!httpResponse.IsSuccessStatusCode)
                {
                    string errorContent = await DecompressContentAsync(httpResponse.Content);
                    return StatusCode((int)httpResponse.StatusCode, new 
                    { 
                        Message = "Ошибка платежного шлюза Ozon Pay", 
                        Details = errorContent 
                    });
                }

                // 7. Обработка УСПЕШНОГО ОТВЕТА (200 OK)
                string successContent = await DecompressContentAsync(httpResponse.Content);

                try
                {
                    var ozonResponse = JsonSerializer.Deserialize<CreatePaymentOzonResponse>(successContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (ozonResponse == null)
                    {
                        return StatusCode(500, "Критическая ошибка десериализации ответа Ozon.");
                    }

                    // ЕСЛИ ССЫЛКА ЕСТЬ — ОТДАЕМ ЕЁ СРАЗУ
                    if (ozonResponse.Order != null && !string.IsNullOrEmpty(ozonResponse.Order.PayLink))
                    {
                        return Ok(new 
                        {
                            Message = "Успех! Ссылка получена.",
                            PayUrl = ozonResponse.Order.PayLink,
                            OrderId = ozonResponse.Order.Id
                        });
                    }

                    return Ok(ozonResponse);
                }
                catch (JsonException)
                {
                    return StatusCode(500, new 
                    { 
                        Message = "Не удалось распарсить ответ Озона как JSON", 
                        RawContent = successContent 
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера", Error = ex.Message });
            }
        }

        // БРОНЕБОЙНЫЙ метод автоматической распаковки (проверяет подстроки без учета регистра)
        private async Task<string> DecompressContentAsync(HttpContent content)
        {
            var encodings = content.Headers.ContentEncoding.Select(e => e.ToLowerInvariant()).ToList();

            if (encodings.Any(e => e.Contains("br")))
            {
                using var responseStream = await content.ReadAsStreamAsync();
                using var decompressionStream = new BrotliStream(responseStream, CompressionMode.Decompress);
                using var reader = new System.IO.StreamReader(decompressionStream, Encoding.UTF8);
                return await reader.ReadToEndAsync();
            }
            if (encodings.Any(e => e.Contains("gzip")))
            {
                using var responseStream = await content.ReadAsStreamAsync();
                using var decompressionStream = new GZipStream(responseStream, CompressionMode.Decompress);
                using var reader = new System.IO.StreamReader(decompressionStream, Encoding.UTF8);
                return await reader.ReadToEndAsync();
            }

            return await content.ReadAsStringAsync();
        }

        private List<OzonPaymentItem> UpdateItemsCurrency(List<OzonPaymentItem> items)
        {
            if (items == null) return new List<OzonPaymentItem>();
            
            var updatedList = new List<OzonPaymentItem>();
            foreach (var item in items)
            {
                updatedList.Add(item with 
                { 
                    Price = item.Price with { CurrencyCode = "643" } 
                });
            }
            return updatedList;
        }
    }
}
