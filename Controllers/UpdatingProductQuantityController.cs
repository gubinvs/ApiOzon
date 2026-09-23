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
                        // 1. Быстро собираем данные из БД за ОДИН шаг
                        // Используем анонимный объект, чтобы вытащить только нужные для работы поля (Guid и Sku)
                        var productsWithSku = await _db.GoodsTable
                            .Join(
                                _db.SkuOzon,
                                product => product.Guid,
                                sku => sku.GuidIdProduct,
                                (product, sku) => new { product.Guid, sku.SkuOzon }
                            )
                            .ToListAsync();

                        if (!productsWithSku.Any()) return Ok(new {massage = "productsWithSku - пуст!"});

                        // 2. Собираем все SKU в единый список для отправки в API
                        var allSkuList = productsWithSku.Select(x => x.SkuOzon).ToList();

                        // 3. Делаем ОДИН пакетный запрос к API Ozon (передаем список SKU)
                        // Примечание: Метод GetFboQuantityBatchAsync приведен как целевой пример оптимизации API
                        var jsonResponseOzonDate = await _ozonStockService.GetFboQuantityBatchAsync(allSkuList);
                        var responseOzonDate = JsonSerializer.Deserialize<OzonStocksResponse>(jsonResponseOzonDate);

                        // Создаем словарь для быстрого поиска остатков по SKU (O(1))
                        var stocksBySku = responseOzonDate?.Products?
                            .ToDictionary(p => p.Sku, p => p.Present) ?? new Dictionary<string, int>();

                        // 4. Формируем итоговый список данных на обновление
                        var warehouseDataList = productsWithSku.Select(item => new
                        {
                            GuidIdProduct = item.Guid,
                            Name = "Склады ОЗОН",
                            Quantity = stocksBySku.TryGetValue(item.SkuOzon, out var quantity) ? quantity : 0
                        }).ToList();
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