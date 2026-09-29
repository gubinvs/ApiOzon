using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ApiOzon;
using ApiOzon.Models;


/// <summary>
/// Создадим сервис OzonSyncService.cs, который будет отвечать 
/// исключительно за скачивание данных из Ozon API и сохранение их в 
/// базу данных порциями.
/// </summary>
/// 


public class OzonSyncService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OzonSyncService> _logger;

    public OzonSyncService(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        ILogger<OzonSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<int> RunSyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ShopDbContext>(); 
        var client = _httpClientFactory.CreateClient("OzonDeliveryClient");

        int offset = 0;
        int limit = 100; 
        bool hasMore = true;
        int totalSaved = 0;

        _logger.LogInformation("Начало синхронизации ПВЗ Ozon через внешний триггер...");
        

        try
        {
            while (hasMore && !cancellationToken.IsCancellationRequested)
            {
                var requestBody = new DeliveryPointListRequest
                {
                    Type = new List<string> { "pickup" },
                    Pagination = new DeliveryPointPagination { Offset = offset, Limit = limit } // Теперь типы совпадают!
                };



                var listResponse = await client.PostAsJsonAsync("v1/delivery-point/list", requestBody, cancellationToken);
                if (!listResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"Ozon List API вернул ошибку: {listResponse.StatusCode}");
                    break;
                }

                var listData = await listResponse.Content.ReadFromJsonAsync<DeliveryPointListResponse>(cancellationToken: cancellationToken);
                if (listData == null || listData.DeliveryPoints.Count == 0)
                {
                    hasMore = false;
                    break;
                }

                var deliveryPointIds = listData.DeliveryPoints.Select(x => x.DeliveryPointId).Distinct().ToList();
                var infoResponse = await client.PostAsJsonAsync("v1/delivery-point/info", new DeliveryPointInfoRequest { DeliveryPointIds = deliveryPointIds }, cancellationToken);
                if (!infoResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"Ozon Info API вернул ошибку: {infoResponse.StatusCode}");
                    break;
                }

                var infoData = await infoResponse.Content.ReadFromJsonAsync<DeliveryPointInfoResponse>(cancellationToken: cancellationToken);
                if (infoData == null) break;

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
                offset += limit;

                // Небольшой таймаут (500мс) бережет лимиты Ozon API внутри Docker-контейнера
                await Task.Delay(500, cancellationToken); 
            }

            _logger.LogInformation($"Синхронизация успешно завершена. Сохранено точек: {totalSaved}");
            return totalSaved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка при синхронизации ПВЗ Ozon");
            throw;
        }
    }
}
