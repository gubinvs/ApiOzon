using ApiOzon.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace ApiOzon
{   
    [ApiController]
    [Route("v1/[controller]")]
    public class AuthTokenController : ControllerBase
    {
        private readonly IOzonAuthService _authService;

        // Внедряем сервис авторизации через DI
        public AuthTokenController(IOzonAuthService authService)
        {
            _authService = authService;
        }
            
        [HttpPost]
        public async Task<IActionResult> ReturnAuthToken()
        {
            try
            {
                // Сервис сам решит: взять токен из кэша или сделать запрос к Ozon (с обходом testcookie)
                string token = await _authService.GetTokenAsync();
                
                return Ok(new { accessToken = token });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка получения токена: {ex.Message}" });
            }
        }
    }
}
