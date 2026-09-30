using System.Net;
using System.Net.Http.Headers;

namespace ApiOzon.Services
{
    public class OzonAuthHandler : DelegatingHandler
    {
        private readonly IOzonAuthService _authService;

        public OzonAuthHandler(
            IOzonAuthService authService)
        {
            _authService = authService;
        }


        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // ==================================================
            // Получаем текущий токен
            // ==================================================

            string token = await _authService.GetTokenAsync();


            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // ==================================================
            // Client-Id
            // ==================================================

            request.Headers.Remove("Client-Id");
            request.Headers.Remove("client-id");
            request.Headers.Remove("Api-Key");
            request.Headers.Add("Client-Id","5755054");

            // ==================================================
            // Отправляем запрос
            // ==================================================

            var response = await base.SendAsync(request, cancellationToken);

            // ==================================================
            // Всё нормально
            // ==================================================

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            // ==================================================
            // Получили 401
            // ==================================================

            Console.WriteLine("OZON AUTH: получен 401. " + "Обновляем токен...");
            response.Dispose();

            // ==================================================
            // Получаем НОВЫЙ токен
            // ==================================================

            string newToken = await _authService.RefreshTokenAsync();

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

            // ==================================================
            // Повторяем запрос ОДИН раз
            // ==================================================

            Console.WriteLine("OZON AUTH: повторяем запрос " + "с новым токеном");

            return await base.SendAsync(request,cancellationToken);
        }
    }
}
