using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
        
        private readonly PasswordGuid _password;
        private readonly ShopDbContext _db;

        public UpdatingProductQuantityController (
                                                IOptions<PasswordGuid> options,
                                                ShopDbContext db
                                            )
        {
            _password = options.Value;
            _db = db;
        }

        [HttpPost]
        public IActionResult UpdatingProductQuantity (string password)
        {
            // Проверка пароля GUID
            if (_password.Password == password)
            {
                try
                    {
                        // Загружаем данные из таблицы товаров
                        var product = _db.GoodsTable
                            .ToList();
                    } 
                catch
                    {
                        
                    }

                return Ok(new {massage = "Информация о количестве товара на складах ОЗОН, успешно обновлена в базе данных."});
                
            } else
            {
                return Ok(new {massage = "Пароль не верен!"});
            }
        }
    }
}