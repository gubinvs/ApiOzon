using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ApiOzon 
{
    [ApiController]
    [Route("v1/[controller]")]
    public class UpdatingProductQuantityController : ControllerBase
    {
        /// Контроллер принимает идентификатор GUID (своего рода пароль) и выполняет следующие функции:
        /// - Скачивает из базы данных перечень товаров магазина;
        /// - На основе их GUID сопоставляет данные с таблицей, которая содержит SKU товара в системе ОЗОН
        /// Далее поочередно:
        /// - Делает запрос на сервер OZON и получет количество товара нашего магазина на складах FBO OZON
        /// - Обновляет количество товарв таблице базы данных магазина
        /// Если работа контроллера завершилась ошибкой, отправляет сообщение на почту администратора
        /// 
        
        private readonly IOzonStockService _ozonStockService;
        private readonly PasswordGuid _password;
        private readonly ShopDbContext _db;

        public UpdatingProductQuantityController (
                                                IOzonStockService ozonStockService,
                                                IOptions<PasswordGuid> options,
                                                ShopDbContext db
                                            )
        {
            _ozonStockService = ozonStockService;
            _password = options.Value;
            _db = db;
        }


        [HttpPost]
        public async Task<IActionResult> UpdatingProductQuantity (string password)
        {

            // Проверка пароля GUID
            if (_password.Password == password)
            {
                try {
                        // 1. Извлекаем данные из API (код из предыдущего шага)
                        var productsWithSku = await _db.GoodsTable
                            .Join(
                                _db.SkuOzon,
                                product => product.Guid,
                                sku => sku.GuidIdProduct,
                                (product, sku) => new { product.Guid, sku.SkuOzon }
                            )
                            .ToListAsync();

                        // Используем строго типизированную структуру вместо анонимного объекта для удобства
                        var currentOzonStocks = new System.Collections.Concurrent.ConcurrentBag<(string ProductGuid, int Quantity)>();
                        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 10 };

                        await Parallel.ForEachAsync(productsWithSku, parallelOptions, async (item, cancellationToken) =>
                        {
                            try
                            {
                                var jsonResponseOzonDate = await _ozonStockService.GetFboQuantityAsync(item.SkuOzon);
                                if (!string.IsNullOrEmpty(jsonResponseOzonDate))
                                {
                                    var responseOzonDate = JsonSerializer.Deserialize<OzonStocksResponse>(jsonResponseOzonDate);
                                    int quantity = responseOzonDate?.Products?[0]?.Present ?? 0;
                                    
                                    currentOzonStocks.Add((item.Guid, quantity));
                                }
                            }
                            catch (Exception)
                            {
                                // Логирование ошибок сети/API
                            }
                        });

                        // =========================================================================
                        // НОВЫЙ БЛОК: СОХРАНЕНИЕ / ОБНОВЛЕНИЕ В БАЗЕ ДАННЫХ
                        // =========================================================================

                        if (!currentOzonStocks.Any()) return Ok(new {massage = "currentOzonStocks - null!"});

                        // 1. ИСПРАВЛЕНО: Обращаемся к .ProductGuid, как указано в кортеже выше
                        var productIds = currentOzonStocks.Select(x => x.ProductGuid).ToList();

                        // 2. Получаем существующие записи из БД
                        var existingWarehouseRecords = await _db.Warehouse
                            .Where(w => productIds.Contains(w.GuidIdProduct) && w.Name == "Склады ОЗОН")
                            .ToDictionaryAsync(w => w.GuidIdProduct);

                        // 3. Распределяем: что обновить, а что добавить
                        foreach (var stock in currentOzonStocks)
                        {
                            // ИСПРАВЛЕНО: Здесь тоже используем имя stock.ProductGuid
                            if (existingWarehouseRecords.TryGetValue(stock.ProductGuid, out var existingRecord))
                            {
                                if (existingRecord.Quantity != stock.Quantity) 
                                {
                                    existingRecord.Quantity = stock.Quantity;
                                    _db.Warehouse.Update(existingRecord); 
                                }
                            }
                            else
                            {
                                var newRecord = new WarehouseDb
                                {
                                    GuidIdProduct = stock.ProductGuid, // Присваиваем строковое значение в модель
                                    Name = "Склады ОЗОН",
                                    Quantity = stock.Quantity
                                };
                                await _db.Warehouse.AddAsync(newRecord);
                            }
                        }

                        // 4. Сохраняем одним пакетом
                        await _db.SaveChangesAsync();
                } 
                catch {
                    
                    // Отправляем сообщение на почту о том, что работа контроллера завершилась с ошибкой

                    





                    return Ok(new {massage = "Работа контроллера, обновление количестве товара на складах ОЗОН, завершилось с ошибкой."});
                }

                return Ok(new {massage = "Информация о количестве товара на складах ОЗОН, успешно обновлена в базе данных."});
                
            } else
            {
                return Ok(new {massage = "Пароль не верен!"});
            }
        }
    }
}