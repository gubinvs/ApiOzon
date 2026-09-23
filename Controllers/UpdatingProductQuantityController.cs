using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
                        // 1. Оптимизируем БД: получаем только нужные пары (Product Guid -> Sku) за ОДИН запрос
                        var productsWithSku = await _db.GoodsTable
                            .Join(
                                _db.SkuOzon,
                                product => product.Guid,
                                sku => sku.GuidIdProduct,
                                (product, sku) => new { product.Guid, sku.SkuOzon }
                            )
                            .ToListAsync();

                        // 2. Создаем потокобезопасную коллекцию для сбора результатов
                        var warehouseDataList = new System.Collections.Concurrent.ConcurrentBag<object>();

                        // 3. Запускаем параллельную обработку API-запросов (например, по 10 одновременно, чтобы не спамить Ozon)
                        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 10 };

                        await Parallel.ForEachAsync(productsWithSku, parallelOptions, async (item, cancellationToken) =>
                        {
                            try
                            {
                                // Используем ваш ОРИГИНАЛЬНЫЙ метод, который принимает один SKU
                                var jsonResponseOzonDate = await _ozonStockService.GetFboQuantityAsync(item.SkuOzon);
                                
                                if (!string.IsNullOrEmpty(jsonResponseOzonDate))
                                {
                                    var responseOzonDate = JsonSerializer.Deserialize<OzonStocksResponse>(jsonResponseOzonDate);
                                    
                                    warehouseDataList.Add(new
                                    {
                                        GuidIdProduct = item.Guid,
                                        Name = "Склады ОЗОН",
                                        Quantity = responseOzonDate?.Products?[0]?.Present ?? 0
                                    });
                                }
                            }
                            catch (Exception)
                            {
                                // Здесь стоит обработать ошибку (например, логировать таймаут или ошибку сети)
                            }
                        });
                        
                        // Записываем данные в базу
                        
                        // return Ok(warehouseDataList);
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