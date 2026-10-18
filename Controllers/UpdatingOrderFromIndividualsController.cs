using ApiOzon.Models;
using Microsoft.AspNetCore.Mvc;


namespace ApiOzon.Controllers
{
    /// <summary>
    /// Котроллер принимает данные о заказе физичиских лиц и обновляет данные в базе
    /// </summary>
    [Controller]
    [Route("v1/[controller]")]
    public class UpdatingOrderFromIndividualsController : ControllerBase
    {
        private readonly ShopDbContext _db;

        public UpdatingOrderFromIndividualsController (
            ShopDbContext db
        )
        {
            _db = db;
        }

        [HttpPost]
        public IActionResult UpdatingOrder([FromBody] UpdatingOrderFromIndividualsReguaest data) 
        { 
            if (data == null) 
            {
                return BadRequest("Данные не переданы.");
            }

            // 1. Ищем существующую запись в базе данных по ID
            var existingOrder = _db.OrdersFromIndividuals.FirstOrDefault(x => x.Id == data.Id);

            // 2. Проверяем, нашли ли мы запись
            if (existingOrder == null)
            {
                return NotFound($"Заказ с ID {data.Id} не найден.");
            }

            // 3. Обновляем только нужные поля найденного объекта
            // Если пришел null ИЛИ строка "string" ИЛИ пустота — мы НЕ перезаписываем телефон
            if (!(data.PhoneUser == null || data.PhoneUser == "string" || string.IsNullOrWhiteSpace(data.PhoneUser)))
            {
                existingOrder.PhoneUser = data.PhoneUser; 
            }
            
            // Здесь можно обновить и другие поля, если они есть в data
            if (data.StatusOrder != "string")
            {
               existingOrder.StatusOrder = data.StatusOrder; 
            }

            if (data.Amount != null)
            {
               existingOrder.Amount = data.Amount; 
            }

            if (data.DateTime != null)
            {
               existingOrder.DateTime = data.DateTime; 
            }

            if (data.ProductName != null)
            {
               existingOrder.ProductName = data.ProductName; 
            }
            

            // 4. Сохраняем изменения в базе данных
            _db.SaveChanges(); 

            return Ok(new { 
                id = existingOrder.Id, 
                phoneUser = existingOrder.PhoneUser,
                statusOrder = data.StatusOrder
            });
        }

    }
}