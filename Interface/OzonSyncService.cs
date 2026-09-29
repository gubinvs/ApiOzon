using System.Net.Http.Headers;
using ApiOzon.Models;
using Microsoft.Extensions.Options;

namespace ApiOzon.Services
{
    public class OzonSyncService
    {
        private readonly OzonDeliveryParam _ozonParam;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ShopDbContext _db;

        // Внедряем зависимости прямо в сервис
        public OzonSyncService (
            IOptions<OzonDeliveryParam> options,
            IHttpClientFactory httpClientFactory,
            ShopDbContext db
        )
        {
            _ozonParam = options.Value;
            _httpClientFactory = httpClientFactory;
            _db = db;
        }

        

        public async Task<int> RunSyncAsync()
        {
            int off = 1; // После цикла меняем на lim, а к lim прибавляем 100 
            int lim = 100; 
            bool hasMore = true;
            int totalSaved = 0;

            var requestBody = new { 
                type = new[] { "1" }, 
                pagination = new { offset = off, limit = lim } 
            };

            var httpClient = _httpClientFactory.CreateClient("OzonDeliveryClient");
            var url = $"{_ozonParam.host}/v1/delivery-point/list";


            try
            {
                while (hasMore)
                {
                 
                    // Отправляем POST-запрос с токеном
                    var listResponse = await httpClient.PostAsJsonAsync(url, requestBody);
                    
                    if (!listResponse.IsSuccessStatusCode)
                    {
                        var errBody = await listResponse.Content.ReadAsStringAsync();
                        break;
                    }

                    var listData = await listResponse.Content.ReadFromJsonAsync<DeliveryPointListResponse>();
                    if (listData == null || listData.DeliveryPoints == null || listData.DeliveryPoints.Count == 0)
                    {
                        hasMore = false;
                        break;
                    }

                    // Сбор числовых ID для детального info
                    var deliveryPointIds = listData.DeliveryPoints
                        .Select(x => x.DeliveryPointId)
                        .Distinct()
                        .ToList();
                    
                    var infoRequestBody = new DeliveryPointInfoRequest
                    {
                        DeliveryPointIds = deliveryPointIds
                    };

                    var infoResponse = await httpClient.PostAsJsonAsync("v1/delivery-point/info", infoRequestBody);
                    
                    if (!infoResponse.IsSuccessStatusCode)
                    {
                        var errBody = await infoResponse.Content.ReadAsStringAsync();
                        break;
                    }

                    var infoData = await infoResponse.Content.ReadFromJsonAsync<DeliveryPointInfoResponse>();
                    if (infoData == null || infoData.DeliveryPoints == null) break;

                    // Мёрджим данные List + Info и маппим в модель базы данных MySQL
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

                    // Upsert в MySQL таблицу
                    foreach (var entity in mappedEntities)
                    {
                        var existing = await _db.OzonDeliveryPoints.FindAsync(entity.DeliveryPointId);
                        if (existing != null)
                        {
                            _db.Entry(existing).CurrentValues.SetValues(entity);
                        }
                        else
                        {
                            await _db.OzonDeliveryPoints.AddAsync(entity);
                        }
                    }

                    await _db.SaveChangesAsync();

                    totalSaved += mappedEntities.Count;
                    off += lim; // Двигаем страницу пагинации вперед

                    // Пауза 500мс для соблюдения Rate Limits
                    await Task.Delay(500);
                }

            
                return totalSaved;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
