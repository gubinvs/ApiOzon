using ApiOzon.Models;
using Microsoft.AspNetCore.Mvc;

namespace ApiOzon
{
    [ApiController]
    [Route("v1/[controller]")]
    public class DeliveryCheckoutController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DeliveryCheckoutController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(
            [FromBody] DeliveryCheckoutRequest request)
        {
            var client = _httpClientFactory
                .CreateClient("OzonDeliveryClient");

            var response = await client.PostAsJsonAsync(
                "v1/order/checkout",
                request);

            var result = await response.Content
                .ReadAsStringAsync();

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                ContentType = "application/json",
                Content = result
            };
        }
    }
}