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
        [HttpPost]
        public IActionResult PointCheck ([FromBody] DeliveryPointCheckAvailabilityReguest reguest)
        {
            

            return Ok();
        }
        
    }
}