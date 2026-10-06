using Microsoft.AspNetCore.Mvc;
using ApiOzon.Services;
using ApiOzon.Core;
using Microsoft.Extensions.Options;

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OzonSyncController : ControllerBase
    {
        private readonly OzonSyncTrigger _trigger;
        private readonly PasswordGuid _password;

        public OzonSyncController(OzonSyncTrigger trigger, IOptions<PasswordGuid> options)
        {
            _trigger = trigger;
            _password = options.Value;
        }

        [HttpPost("start")]
        public IActionResult StartSync([FromQuery] string password)
        {
            if (password != _password.Password)
            {
                return Unauthorized("Неверный пароль доступа.");
            }

            // Отправляем сигнал воркеру
            _trigger.FireSync();

            return Ok("Запрос на запуск синхронизации принят. Воркер начал работу в фоне.");
        }
    }
}
