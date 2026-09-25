using ApiOzon.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class DeliveryPointListController : ControllerBase
    {
        private readonly HttpClient _ozonClient;

        // Внедряем преднастроенный клиент через Factory. 
        // Наш OzonAuthHandler сам прикрепит живой токен к этому запросу!
        public DeliveryPointListController(IHttpClientFactory httpClientFactory)
        {
            _ozonClient = httpClientFactory.CreateClient("OzonDeliveryClient");
        }

        /// <summary>
        /// Получает список доступных пунктов выдачи Ozon Доставки
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetPoints([FromBody] OzonDeliveryPointRequest? filter)
        {
            try
            {
                // Если фильтры не переданы, отправляем пустой объект {}, Ozon вернет все ПВЗ
                var requestBody = filter ?? new OzonDeliveryPointRequest();

                // Делаем POST-запрос на относительный адрес. 
                // Базовый адрес (https://ozon.ru) подставится из Program.cs
                HttpResponseMessage response = await _ozonClient.PostAsJsonAsync("v1/delivery-point/list", requestBody);

                // Если Ozon вернул ошибку (например, 400 или 401)
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new 
                    { 
                        message = "Ошибка при запросе к API Ozon Доставки", 
                        details = errorContent 
                    });
                }

                // Читаем успешный JSON-ответ от Ozon и отдаем его клиенту
                var jsonResult = await response.Content.ReadFromJsonAsync<object>();
                return Ok(jsonResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    message = "Внутренняя ошибка сервера при получении ПВЗ", 
                    details = ex.Message 
                });
            }
        }
    }
}
