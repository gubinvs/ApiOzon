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

        public DeliveryPointListController(IHttpClientFactory httpClientFactory)
        {
            _ozonClient = httpClientFactory.CreateClient("OzonDeliveryClient");
        }

        /// <summary>
        /// Получает строго типизированный список доступных пунктов выдачи Ozon Доставки
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetPoints([FromBody] OzonDeliveryPointRequest? filter)
        {
            try
            {
                var requestBody = filter ?? new OzonDeliveryPointRequest();

                HttpResponseMessage response = await _ozonClient.PostAsJsonAsync("v1/delivery-point/list", requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new 
                    { 
                        message = "Ошибка при запросе к API Ozon Доставки", 
                        details = errorContent 
                    });
                }

                // Десериализуем ответ в строгую C#-модель
                var result = await response.Content.ReadFromJsonAsync<OzonDeliveryPointResponse>();
                return Ok(result);
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
