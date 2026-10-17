using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiOzon.Core;
using Microsoft.Extensions.Options;
using System.IO.Compression; 


namespace ApiOzon.Controllers
{

    [Controller]
    [Route("v1/[controller]")]
    public class ReturnGuidOrderIndividualsController : ControllerBase
    {
        
        private readonly ShopDbContext _db;

        public  ReturnGuidOrderIndividualsController (
            ShopDbContext db
        )
        {
            _db = db;
        }

        [HttpPost]
        public IActionResult ReturnGuidOrder (int idOrder)
        {
            var guid = _db.OrdersFromIndividuals
                    .FirstOrDefault(x => x.Id == idOrder);
            
            if(guid == null) {return BadRequest("Данные не переданы.");}
            
            return Ok(guid.GuidIdOrder);
        }
    }
}