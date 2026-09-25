using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ApiOzon.Services
{
    public class OzonAuthHandler : DelegatingHandler
    {
        private readonly IOzonAuthService _authService;
        private readonly OzonDeliveryParam _deliveryParam;

        public OzonAuthHandler(IOzonAuthService authService, IOptions<OzonDeliveryParam> options)
        {
            _authService = authService;
            _deliveryParam = options.Value;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 1. Получаем живой OAuth-токен
            string token = await _authService.GetTokenAsync();

            // 2. Устанавливаем стандартную Bearer авторизацию
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 3. Очищаем заголовки перед повторной записью
            request.Headers.Remove("Client-Id");
            request.Headers.Remove("client-id");
            request.Headers.Remove("X-Client-Id");
            request.Headers.Remove("Api-Key");
            request.Headers.Remove("X-Client-Secret");

            // 4. Передаем идентификатор клиента в Ozon Доставку (пробуем основные форматы XAPI)
            request.Headers.Add("Client-Id", _deliveryParam.client_id);
            request.Headers.Add("client-id", _deliveryParam.client_id);
            
            // Передаем секрет клиента в специализированном заголовке для OAuth-шлюзов
            request.Headers.Add("X-Client-Secret", _deliveryParam.client_secret);

            // 5. Отправляем запрос на api-delivery.ozon.ru
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
