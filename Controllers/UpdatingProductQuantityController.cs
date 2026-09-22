using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Threading.Tasks;

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
                    // Загружаем данные из таблицы товаров
                    var product = _db.GoodsTable
                        .ToList();
                    
                    // Перебирая массив данных товара:
                    foreach (var item in product)
                    {
                   
                        // Находим в таблице соответствующие записи по GuidIdProduct
                        var sku = _db.SkuOzon
                                .Where(e => e.GuidIdProduct == item.Guid)
                                .FirstOrDefault();
                        
                        if (sku != null)
                        {
                            // Делаем запрос api OZON для получения данных о наличии товара на складе ОЗОН
                            var stock = await _ozonStockService.GetFboQuantityAsync(sku.SkuOzon);
                            
                            // Десериализуем в класс
                            var d = JsonSerializer.Deserialize<OzonStocksResponse>(stock);

                            return Ok (new {d});

                        } else {break;}
                    }
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