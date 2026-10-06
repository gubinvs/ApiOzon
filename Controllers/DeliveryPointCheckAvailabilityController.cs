using Microsoft.AspNetCore.Mvc;
using ApiOzon.Models;

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
        public IActionResult PointCheck ([FromBody] DeliveryPointCheckAvailabilityReguest reguest)
        {

            var client = _httpClientFactory.CreateClient("OzonDeliveryClient");
            

            return Ok();
        }
    }
}