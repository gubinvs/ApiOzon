using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class DeliveryPointSearchController : ControllerBase
    {
        private readonly ShopDbContext _db;

        public DeliveryPointSearchController(ShopDbContext db) {_db = db;}


        [HttpPost]
        public async Task<IActionResult> Search([FromBody] SearchRequestDto request, CancellationToken cancellationToken)
        {
            // Очищаем и переводим в нижний регистр для безопасного поиска
            string searchString = (request?.Query ?? "").Trim().ToLower();

            // Базовый запрос: выбираем только активные ПВЗ
            var queryable = _db.OzonDeliveryPoints.Where(x => x.IsActive);

            // Если пользователь что-то ввел, фильтруем по адресу, названию или номеру ПВЗ
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                queryable = queryable.Where(x => 
                (x.Address != null && x.Address.ToLower().Contains(searchString)) || 
                (x.Name != null && x.Name.ToLower().Contains(searchString)) || 
                (x.DeliveryPointNumber != null && x.DeliveryPointNumber.ToLower().Contains(searchString))
            );
            }

            // Выполняем запрос с ограничением, чтобы не перегружать сеть и React (например, максимум 50 точек)
            var dbPoints = await queryable.Take(50).ToListAsync(cancellationToken);

            // Маппим (переименовываем) свойства из формата БД в формат, который ожидает React
            var result = dbPoints.Select(p => new
            {
                delivery_point_id = p.DeliveryPointId,
                delivery_point_number = p.DeliveryPointNumber,
                name = p.Name,
                address = p.Address,
                lat = p.Latitude,
                lng = p.Longitude
            }).ToList();

            return Ok(result);
        }
    }
}