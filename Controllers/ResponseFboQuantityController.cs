using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ApiOzon
{
    [ApiController]
    [Route("v1/[controller]")]
    public class ResponseFboQuantityController : ControllerBase
    {
        // Внедряем только наш новый сервис
        private readonly IOzonStockService _ozonStockService;

        public ResponseFboQuantityController(IOzonStockService ozonStockService)
        {
            _ozonStockService = ozonStockService;
        }

        [HttpPost]
        public async Task<IActionResult> ResponseFboQuantity(string skuProduct)
        {
            try
            {
                // Вызываем логику из сервиса
                var jsonResult = await _ozonStockService.GetFboQuantityAsync(skuProduct);

                return new ContentResult
                {
                    StatusCode = 200,
                    Content = jsonResult,
                    ContentType = "application/json"
                };
            }
            catch (HttpRequestException httpEx)
            {
                // Обработка ошибок, если Ozon вернул 400, 403 или 500 статус
                return StatusCode((int?)httpEx.StatusCode ?? 502, new
                {
                    message = "Ошибка при запросе к API Ozon",
                    error = httpEx.Message
                });
            }
            catch (Exception ex)
            {
                // Любые другие непредвиденные ошибки
                return StatusCode(500, new
                {
                    message = "Внутренняя ошибка сервера",
                    error = ex.Message
                });
            }
        }
    }
}
