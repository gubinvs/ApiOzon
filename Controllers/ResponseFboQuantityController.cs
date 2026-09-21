using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApiOzon
{
    
    /// <summary>
    /// Контроллер принимает в запросе SKU в системе озон, 
    /// соответствующему товару и ответом возвращает в том числе количество, которое сейчас находится в продаже на стоках ОЗОН
    /// Этот контроллер необходим для запроса количества товаров на складах ОЗОН
    /// 
    /// Response body
    /// 
    /// {
    ///     "products": [
    ///         {
    ///             "sku": 3225715829,
    ///             "offer_id": "LC1D18M7",
    ///             "product_id": 3217113853,
    ///             "warehouse_id": 23903599483000,
    ///             "present": 3,
    ///             "reserved": 0
    ///         }
    ///     ],
    ///     "has_next": false,
    ///     "cursor": ""
    /// }
    /// </summary>
    /// 
    


    [ApiController]
    [Route("v1/[controller]")]
    public class ResponseFboQuantityController : ControllerBase
    {
        private readonly OzonSellerParam _ozonParam;
        private readonly IHttpClientFactory _httpClientFactory;

        public ResponseFboQuantityController(
            IOptions<OzonSellerParam> options,
            IHttpClientFactory httpClientFactory)
        {
            _ozonParam = options.Value;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> ResponseFboQuantity(string skuProduct)
        {

            var dataOzon = new
            {
                limit = 1,
                skus = new[]
                {
                    skuProduct
                }
            };

            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                // Адрес сервера Ozon
                var url =
                    $"{_ozonParam.UrlOzonApiAdress}/v1/product/info/stocks-by-warehouse/fbo";

                // Создаём запрос
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    url
                );

                // Заголовки
                request.Headers.Add(
                    "Client-Id",
                    _ozonParam.SellerClientId.ToString()
                );

                request.Headers.Add(
                    "Api-Key",
                    _ozonParam.SellerApiKey
                );

                // Преобразуем объект в JSON
                var jsonData = JsonSerializer.Serialize(dataOzon);

                // Тело запроса
                request.Content = new StringContent(
                    jsonData,
                    Encoding.UTF8,
                    "application/json"
                );

                // Отправляем запрос Ozon
                using var response =
                    await httpClient.SendAsync(request);

                // Читаем ответ Ozon
                var responseBody =
                    await response.Content.ReadAsStringAsync();

                // Возвращаем оригинальный ответ Ozon
                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    Content = responseBody,
                    ContentType = "application/json"
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ошибка запроса Ozon",
                    error = ex.Message
                });
            }
        }
    }
}