using ApiOzon.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace ApiOzon.Services
{
    public class OzonSyncService(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<OzonSyncService> logger,
        IConfiguration configuration) // Инжектируем конфигурацию для чтения appsettings
    {
        public async Task<int> RunSyncAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ShopDbContext>(); 
            
            // 1. Получаем конфигурационные данные
            var authUrl = configuration["OzonDelivery:auth_url"] ?? "https://xapi.ozon.ru/oauth/token";
            var clientId = configuration["OzonDelivery:client_id"];
            var clientSecret = configuration["OzonDelivery:client_secret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("В appsettings.json не заполнены client_id или client_secret в секции OzonDelivery.");
            }

            // 2. Шаг авторизации: Получаем OAuth Bearer токен
            logger.LogInformation("Запрос OAuth токена у Ozon...");
            using var authClient = httpClientFactory.CreateClient();
            
            var tokenRequestBody = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                grant_type = "client_credentials",
                scope = "delivery" // ИСПРАВЛЕНО: Добавлен обязательный scope для логистического API Ozon
            };

            var tokenResponse = await authClient.PostAsJsonAsync(authUrl, tokenRequestBody, cancellationToken);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var tokenErr = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError($"Не удалось получить OAuth токен. Статус: {tokenResponse.StatusCode}. Ответ: {tokenErr}");
                throw new HttpRequestException($"Ошибка авторизации Ozon OAuth: {tokenResponse.StatusCode}");
            }

            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<OzonTokenResponse>(cancellationToken: cancellationToken);
            if (tokenData == null || string.IsNullOrEmpty(tokenData.AccessToken))
            {
                throw new InvalidOperationException("Ozon вернул пустой access_token.");
            }

            logger.LogInformation("OAuth токен успешно получен. Запуск синхронизации ПВЗ...");

            // 3. Настраиваем основной клиент для запросов к ПВЗ
            var client = httpClientFactory.CreateClient("OzonDeliveryClient");
            
            // Принудительно устанавливаем Bearer-авторизацию
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
            
            // На всякий случай очищаем старые заголовки Seller API, если они добавлялись хэндлером
            client.DefaultRequestHeaders.Remove("Client-Id");
            client.DefaultRequestHeaders.Remove("Api-Key");

            int offset = 0;   
            int limit = 100;  
            bool hasMore = true;
            int totalSaved = 0;

            try
            {
                while (hasMore && !cancellationToken.IsCancellationRequested)
                {
                    var requestBody = new DeliveryPointListRequest
                    {
                        Type = new List<string> { "1" }, // "1" соответствует ПВЗ в Delivery API
                        Pagination = new DeliveryPointPagination { Offset = offset, Limit = limit }
                    };

                    var listResponse = await client.PostAsJsonAsync("v1/delivery-point/list", requestBody, cancellationToken);
                    
                    if (!listResponse.IsSuccessStatusCode)
                    {
                        var errBody = await listResponse.Content.ReadAsStringAsync(cancellationToken);
                        logger.LogError($"Ozon List API вернул ошибку: {listResponse.StatusCode}. Ответ: {errBody}");
                        break;
                    }

                    var listData = await listResponse.Content.ReadFromJsonAsync<DeliveryPointListResponse>(cancellationToken: cancellationToken);
                    if (listData == null || listData.DeliveryPoints == null || listData.DeliveryPoints.Count == 0)
                    {
                        hasMore = false;
                        break;
                    }

                    var deliveryPointIds = listData.DeliveryPoints
                        .Select(x => x.DeliveryPointId)
                        .Distinct()
                        .ToList();
                    
                    var infoRequestBody = new DeliveryPointInfoRequest
                    {
                        DeliveryPointIds = deliveryPointIds
                    };

                    var infoResponse = await client.PostAsJsonAsync("v1/delivery-point/info", infoRequestBody, cancellationToken);
                    
                    if (!infoResponse.IsSuccessStatusCode)
                    {
                        var errBody = await infoResponse.Content.ReadAsStringAsync(cancellationToken);
                        logger.LogError($"Ozon Info API вернул ошибку: {infoResponse.StatusCode}. Ответ: {errBody}");
                        break;
                    }

                    var infoData = await infoResponse.Content.ReadFromJsonAsync<DeliveryPointInfoResponse>(cancellationToken: cancellationToken);
                    if (infoData == null || infoData.DeliveryPoints == null) break;

                    var mappedEntities = listData.DeliveryPoints
                        .Join(infoData.DeliveryPoints, l => l.DeliveryPointId, i => i.DeliveryPointId, (l, i) => new OzonDeliveryPoint
                        {
                            DeliveryPointId = i.DeliveryPointId.ToString(),
                            DeliveryPointNumber = i.DeliveryPointNumber,
                            Name = i.Name,
                            Address = i.FullAddress,
                            Lat = i.Coordinates.Latitude,
                            Lng = i.Coordinates.Longitude,
                            IsActive = i.IsActive,
                            StoragePeriodDays = i.StoragePeriodDays,
                            FittingRoomsCount = i.FittingRoomsCount,
                            IsBulky = i.IsBulky,
                            MaxWeightG = i.Restrictions.MaxWeightG,
                            MaxWidthMm = i.Restrictions.MaxWidthMm,
                            MaxLengthMm = i.Restrictions.MaxLengthMm,
                            MaxHeightMm = i.Restrictions.MaxHeightMm,
                            
                            MaxPrice = string.IsNullOrWhiteSpace(i.Restrictions.MaxPrice.Amount) 
                                ? 0m 
                                : decimal.Parse(i.Restrictions.MaxPrice.Amount, System.Globalization.CultureInfo.InvariantCulture),
                            
                            ShipmentMethodId = l.ShipmentMethodIds.FirstOrDefault(),
                            LastUpdatedAt = DateTime.UtcNow
                        })
                        .Where(x => x.IsActive)
                        .ToList();

                    foreach (var entity in mappedEntities)
                    {
                        var existing = await dbContext.OzonDeliveryPoints.FindAsync(new object[] { entity.DeliveryPointId }, cancellationToken);
                        if (existing != null)
                        {
                            dbContext.Entry(existing).CurrentValues.SetValues(entity);
                        }
                        else
                        {
                            await dbContext.OzonDeliveryPoints.AddAsync(entity, cancellationToken);
                        }
                    }

                    await dbContext.SaveChangesAsync(cancellationToken);

                    totalSaved += mappedEntities.Count;

                    logger.LogInformation($"Пачка успешно сохранена в MySQL. Текущий offset: {offset}. Накоплено ПВЗ: {totalSaved}");

                    if (listData.DeliveryPoints.Count < limit)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        offset += limit;
                    }

                    await Task.Delay(800, cancellationToken);
                }

                logger.LogInformation($"Синхронизация завершена успешно! Всего добавлено ПВЗ в базу: {totalSaved}");
                return totalSaved;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Критическая ошибка при синхронизации ПВЗ Ozon");
                throw;
            }
        }
    }

    // Вспомогательный класс для десериализации OAuth ответа Ozon
    public class OzonTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
