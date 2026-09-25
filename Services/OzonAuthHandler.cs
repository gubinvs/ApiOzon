using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ApiOzon.Services
{
    public class OzonAuthHandler : DelegatingHandler
    {
        private readonly IOzonAuthService _authService;

        public OzonAuthHandler(IOzonAuthService authService)
        {
            _authService = authService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 1. Берем живой OAuth JWT-токен из кэша
            string token = await _authService.GetTokenAsync();

            // 2. Добавляем стандартную Bearer авторизацию
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 3. Вычищаем заголовки перед записью
            request.Headers.Remove("Client-Id");
            request.Headers.Remove("client-id");
            request.Headers.Remove("Api-Key");

            // 4. ДЛЯ ДОСТАВКИ: В заголовок Client-Id нужно передавать ваш цифровой Seller ID!
            request.Headers.Add("Client-Id", "5755054");

            // 5. Отправляем запрос на api-delivery.ozon.ru
            return await base.SendAsync(request, cancellationToken);
        }
    }
}