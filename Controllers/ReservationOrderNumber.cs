

using ApiOzon.Entities;
using ApiOzon.Core;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Microsoft.Extensions.Options;

namespace ApiOzon.Controllers
{
    /// <summary>
    /// Контроллер принимает GET запрос с паролем, резервирует номер заказа для физ. лиц и возвращает этот номер.
    /// </summary>
    [ApiController] // Лучше использовать ApiController для REST API
    [Route("v1/[controller]")]
    public class ReservationOrderNumberController : ControllerBase
    {
        private readonly PasswordGuid _password;
        private readonly ShopDbContext _db;

        public ReservationOrderNumberController(
            IOptions<PasswordGuid> options, 
            ShopDbContext db
        )
        {
            _password = options.Value;
            _db = db;
        }

        // 1. Меняем на HttpGet, так как JS вызывает его без указания метода
        // 2. Меняем возвращаемый тип на IActionResult, чтобы работал return Ok() или return BadRequest()
        [HttpGet]
        public IActionResult ReservationOrder([FromQuery] string passsw)
        {
            // Проверяем пароль. Если неверный — возвращаем статус 401 (Unauthorized) или 400 (BadRequest)
            if (_password.Password != passsw)
            {
                return BadRequest(new { message = "Неверный пароль" });
            }

            // Записываем заглушку, бронируя номер ОРДЕРА
            var newOrder = new OrdersFromIndividualsDb()
            {
                GuidIdOrder = Guid.NewGuid().ToString(),
                PhoneUser = "dummy",
                StatusOrder = "Не оплачен"
                // Если Id у вас автоинкрементный (Identity) в базе, заполнять его вручную НЕ НАДО.
                // База сама присвоит ему следующий номер при _db.SaveChanges().
            };

            _db.OrdersFromIndividuals.Add(newOrder);
            _db.SaveChanges(); // В этот момент база данных присваивает newOrder.Id реальный номер

            // Возвращаем объект. JS будет читать свойство id
            return Ok(new { id = newOrder.Id, message = "Номер успешно забронирован" });
        }
    }
}