using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ApiOzon.Models;
using ApiOzon.Services;

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class OzonDeliveryPointSyncController : ControllerBase
    {
        private readonly OzonSyncService _syncService;
        private readonly PasswordGuid _password;
        private readonly ShopDbContext _db;
        private readonly ILogger<OzonDeliveryPointSyncController> _logger;

        public OzonDeliveryPointSyncController(
            OzonSyncService syncService,
            IOptions<PasswordGuid> options,
            ShopDbContext db,
            ILogger<OzonDeliveryPointSyncController> logger)
        {
            _syncService = syncService;
            _password = options.Value;
            _db = db;
            _logger = logger;
        }
        
        [HttpPost]
        public async Task<IActionResult> SyncDeliveryPoints([FromQuery] string password, CancellationToken cancellationToken)
        {
            // Проверка вашего внутреннего секретного ключа доступа приложения
            if (_password.Password != password)
            {
                return Unauthorized(new { error = "Неверный секретный ключ доступа" });
            }

            try
            {
                _logger.LogInformation("Запуск фоновой синхронизации ПВЗ Ozon через триггер.");
                
                // Просто вызываем метод без передачи параметров — он всё возьмет из конфигов приложения сам
                int savedCount = await _syncService.RunSyncAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "База данных ПВЗ Ozon успешно обновлена",
                    total_points = savedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка во время синхронизации ПВЗ Ozon.");
                return StatusCode(500, new { error = "Ошибка во время синхронизации.", details = ex.Message });
            }
        }

        [HttpGet("points")]
        public async Task<IActionResult> GetPoints([FromQuery] string? search, [FromQuery] int limit = 50)
        {
            try
            {
                IQueryable<OzonDeliveryPoint> query = _db.OzonDeliveryPoints.Where(x => x.IsActive);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var cleanSearch = search.Trim().ToLower();

                    query = query.Where(x => 
                        EF.Functions.Like(x.Name.ToLower(), $"%{cleanSearch}%") ||
                        EF.Functions.Like(x.Address.ToLower(), $"%{cleanSearch}%") ||
                        EF.Functions.Like(x.DeliveryPointNumber.ToLower(), $"%{cleanSearch}%")
                    );

                    var searchResult = await query.Take(100).ToListAsync();
                    return Ok(new { delivery_points = searchResult });
                }

                var defaultPoints = await query.Take(limit).ToListAsync();
                return Ok(new { delivery_points = defaultPoints });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Ошибка при чтении ПВЗ из базы данных", details = ex.Message });
            }
        }
    }
}
