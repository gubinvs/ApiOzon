
using ApiOzon.Entities;
using ApiOzon.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Microsoft.Extensions.Options;

namespace ApiOzon.Controllers
{
    [Controller]
    [Route("v1/[controller]")]
    public class ReturnOrderFromIndividualsStatusController : ControllerBase
    {
        private readonly ShopDbContext _db;

        public ReturnOrderFromIndividualsStatusController
        (
            ShopDbContext db
        )
        {
            _db = db;
        }

        [HttpPost]
        public IActionResult ReturnOrderStatus (string guid)
        {
            var response = _db.OrdersFromIndividuals.FirstOrDefault(x => x.GuidIdOrder == guid);

            if (response == null ){
                return NotFound($"Заказ с идентификатором {guid} не найден.");
            }

            return Ok(new { 
                number = response.Id,
                amount = response.Amount,
                date = response.DateTime,
                name = response.ProductName,
                status = response.StatusOrder
            });
        }
    }
}