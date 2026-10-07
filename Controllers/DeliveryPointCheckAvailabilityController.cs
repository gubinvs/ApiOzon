using Microsoft.AspNetCore.Mvc;
using ApiOzon.Models;
using System.Threading.Tasks;

namespace ApiOzon.Controllers
{
    /// <summary>
    /// Проверить доступность доставки отправлений в конкретные пункты выдачи OZON
    /// </summary>
    [ApiController]
    [Route("v1/[controller]")]
    public class DeliveryPointCheckAvailabilityController : ControllerBase
    {

        private readonly IHttpClientFactory _httpClientFactory;

        public DeliveryPointCheckAvailabilityController (
            IHttpClientFactory httpClientFactory
        )
        {
            _httpClientFactory = httpClientFactory;
        }
        
        [HttpPost]
        public async Task<IActionResult> PointCheck ([FromBody] DeliveryPointCheckAvailabilityReguest reguest)
        {

            var client =  _httpClientFactory.CreateClient("OzonDeliveryClient");

            var reguestResult = await client.PostAsJsonAsync("/v1/delivery-point/check-availability", reguest);
            var reguestResultJson = await reguestResult.Content.ReadAsStringAsync();

            return Ok(reguestResultJson);
        }
    }
}