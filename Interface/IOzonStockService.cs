using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ApiOzon
{
    // 1. Описываем интерфейс сервиса
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
    public interface IOzonStockService
    {
        /// <summary>
        /// Получает информацию о количестве товаров на складах FBO Ozon по SKU.
        /// Возвращает строку JSON с ответом от Ozon.
        /// </summary>
        Task<string> GetFboQuantityAsync(string skuProduct);
    }

    // 2. Реализуем сервис
    public class OzonStockService : IOzonStockService
    {
        private readonly OzonSellerParam _ozonParam;
        private readonly IHttpClientFactory _httpClientFactory;

        // Внедряем зависимости прямо в сервис
        public OzonStockService(
            IOptions<OzonSellerParam> options,
            IHttpClientFactory httpClientFactory)
        {
            _ozonParam = options.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> GetFboQuantityAsync(string skuProduct)
        {
            var dataOzon = new
            {
                limit = 1,
                skus = new[] { skuProduct }
            };

            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{_ozonParam.UrlOzonApiAdress}/v1/product/info/stocks-by-warehouse/fbo";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            
            // Добавляем заголовки авторизации Ozon
            request.Headers.Add("Client-Id", _ozonParam.SellerClientId.ToString());
            request.Headers.Add("Api-Key", _ozonParam.SellerApiKey);

            var jsonData = JsonSerializer.Serialize(dataOzon);
            request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request);
            
            // Если Ozon ответил ошибкой (4xx, 5xx), выбрасываем исключение
            // чтобы контроллер мог правильно обработать её в try-catch
            response.EnsureSuccessStatusCode(); 

            return await response.Content.ReadAsStringAsync();
        }
    }
}