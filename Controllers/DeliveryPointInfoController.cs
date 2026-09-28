using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/[controller]")]
public class DeliveryPointInfoController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DeliveryPointInfoController(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> Info(
        [FromBody] DeliveryPointInfoRequest request)
    {
        var client = _httpClientFactory
            .CreateClient("OzonDeliveryClient");

        var response = await client.PostAsJsonAsync(
            "v1/delivery-point/info",
            request);

        var result = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"Ozon status: {response.StatusCode}");
        Console.WriteLine($"Ozon response: {result}");

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            ContentType = "application/json",
            Content = result
        };
    }
}