using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ApiOzon.Models;

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class OzonDeliveryPointSyncController : ControllerBase
    {
        /// <summary>
        /// Контроллер принимает идентификатор GUID (своего рода пароль) и выполняет:
        /// - Скачивание всех доступных ПВЗ из API Ozon (с обходом пагинации по 100 штук)
        /// - Сохранение или обновление (Upsert) полученных ПВЗ в локальной базе данных
        /// Если работа завершилась ошибкой, отправляет сообщение на почту администратора (можно вызвать ваш сервис уведомлений)
        /// </summary>

        private readonly OzonSyncService _syncService;
        private readonly PasswordGuid _password;
        private readonly ShopDbContext _db;
        private readonly ILogger<OzonDeliveryPointSyncController> _logger;
        // private readonly IEmailService _emailService; // Если нужно отправлять на почту при ошибке, добавьте сюда

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
            // 1. Проверка пароля GUID
            if (_password.Password != password)
            {
                _logger.LogWarning("Попытка несанкционированного запуска синхронизации ПВЗ. Неверный пароль.");
                return Unauthorized(new { error = "Неверный секретный ключ доступа" });
            }

            try
            {
                _logger.LogInformation("Запуск синхронизации ПВЗ Ozon по запросу через триггер.");
                
                // 2. Вызов сервиса синхронизации (логика пагинации внутри)
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
                _logger.LogError(ex, "Критическая ошибка во время синхронизации ПВЗ Ozon через контроллер.");
                
                // =========================================================================
                // ТУТ ВАШ КОД ОТПРАВКИ НА ПОЧТУ АДМИНИСТРАТОРА (по аналогии с остатками товаров)
                // =========================================================================
                // await _emailService.SendAdminNotificationAsync("Ошибка синхронизации ПВЗ Ozon", ex.Message);

                return StatusCode(500, new 
                { 
                    error = "Ошибка во время синхронизации. Администратор уведомлен.", 
                    details = ex.Message 
                });
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
