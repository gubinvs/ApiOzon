using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace ApiOzon
{
    [ApiController]
    [Route("v1/[controller]")]
    public class DeliveryPointSyncController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PasswordGuid _password;
        private readonly ShopDbContext _db;

        public DeliveryPointSyncController(
            IHttpClientFactory httpClientFactory,
            IOptions<PasswordGuid> options,
            ShopDbContext db)
        {
            _httpClientFactory = httpClientFactory;
             _password = options.Value;
            _db = db;
        }


        [HttpPost]
        public async Task<IActionResult> Sync(string password)
        {
            if (_password.Password != password)
                {
                    return Ok(new {massage = "Пароль не верен!"});
                }


            var client = _httpClientFactory
                .CreateClient("OzonDeliveryClient");

            string? cursor = null;

            int page = 0;

            int total = 0;

            while (true)
            {
                page++;

                // ==========================================
                // 1. Получаем список ПВЗ
                // ==========================================

                string requestJson;

                if (cursor == null)
                {
                    // ПЕРВАЯ СТРАНИЦА

                    requestJson = """
                    {
                        "type": ["1"],
                        "pagination": {
                            "offset": 0,
                            "limit": 100
                        }
                    }
                    """;
                }
                else
                {
                    // ПОСЛЕДУЮЩИЕ СТРАНИЦЫ
                    //
                    // ВАЖНО:
                    // type = номер страницы
                    // offset НЕ передаём

                    var requestObject = new
                    {
                        type = new[]
                        {
                            page.ToString()
                        },

                        pagination = new
                        {
                            limit = 100,
                            cursor = cursor
                        }
                    };

                    requestJson =
                        JsonSerializer.Serialize(
                            requestObject);
                }


                Console.WriteLine(
                    $"OZON: страница {page}");

                Console.WriteLine(
                    $"CURSOR: {cursor ?? "NULL"}");


                var content = new StringContent(
                    requestJson,
                    Encoding.UTF8,
                    "application/json");


                var response = await client.PostAsync(
                    "v1/delivery-point/list",
                    content);


                var responseJson =
                    await response.Content.ReadAsStringAsync();


                if (!response.IsSuccessStatusCode)
                {
                    return new ContentResult
                    {
                        StatusCode =
                            (int)response.StatusCode,

                        ContentType =
                            "application/json",

                        Content = responseJson
                    };
                }


                var listData =
                    JsonSerializer.Deserialize<DeliveryPointListResponse>(
                        responseJson);


                if (listData == null)
                {
                    return BadRequest(
                        "Не удалось разобрать ответ Ozon");
                }


                Console.WriteLine(
                    $"ПВЗ получено: {listData.DeliveryPoints.Count}");


                // ==========================================
                // 2. Получаем ID ПВЗ
                // ==========================================

                var ids = listData
                    .DeliveryPoints
                    .Select(x => x.DeliveryPointId)
                    .Distinct()
                    .ToList();


                // ==========================================
                // 3. Получаем подробную информацию
                // ==========================================

                for (int i = 0; i < ids.Count; i += 100)
                {
                    var batchIds = ids
                        .Skip(i)
                        .Take(100)
                        .ToList();


                    var infoRequest = new
                    {
                        delivery_point_ids = batchIds
                    };


                    var infoJson =
                        JsonSerializer.Serialize(
                            infoRequest);


                    var infoContent =
                        new StringContent(
                            infoJson,
                            Encoding.UTF8,
                            "application/json");


                    var infoResponse =
                        await client.PostAsync(
                            "v1/delivery-point/info",
                            infoContent);


                    var infoResponseJson =
                        await infoResponse.Content
                            .ReadAsStringAsync();


                    if (!infoResponse.IsSuccessStatusCode)
                    {
                        return new ContentResult
                        {
                            StatusCode =
                                (int)infoResponse.StatusCode,

                            ContentType =
                                "application/json",

                            Content =
                                infoResponseJson
                        };
                    }


                    var infoData =
                        JsonSerializer.Deserialize<
                            DeliveryPointInfoResponse>(
                                infoResponseJson);


                    if (infoData == null)
                    {
                        continue;
                    }


                    // ======================================
                    // 4. Сохраняем в БД
                    // ======================================

                    foreach (var point in infoData.DeliveryPoints)
                    {
                        var dbPoint =
                            await _db.OzonDeliveryPoints
                                .FirstOrDefaultAsync(
                                    x =>
                                        x.DeliveryPointId ==
                                        point.DeliveryPointId);


                        if (dbPoint == null)
                        {
                            dbPoint =
                                new OzonDeliveryPointDb
                                {
                                    DeliveryPointId =
                                        point.DeliveryPointId
                                };

                            _db.OzonDeliveryPoints
                                .Add(dbPoint);
                        }


                        dbPoint.DeliveryPointNumber =
                            point.DeliveryPointNumber;

                        dbPoint.Name =
                            point.Name;

                        dbPoint.Address =
                            point.FullAddress;

                        dbPoint.Latitude =
                            point.Coordinates.Latitude;

                        dbPoint.Longitude =
                            point.Coordinates.Longitude;

                        dbPoint.IsActive =
                            point.IsActive;

                        dbPoint.StoragePeriodDays =
                            point.StoragePeriodDays;

                        dbPoint.FittingRoomsCount =
                            point.FittingRoomsCount;

                        dbPoint.IsBulky =
                            point.IsBulky;

                        dbPoint.MaxWeightG =
                            point.Restrictions.MaxWeightG;

                        dbPoint.MaxWidthMm =
                            point.Restrictions.MaxWidthMm;

                        dbPoint.MaxLengthMm =
                            point.Restrictions.MaxLengthMm;

                        dbPoint.MaxHeightMm =
                            point.Restrictions.MaxHeightMm;

                       dbPoint.MaxPrice =
                        decimal.TryParse(
                            point.Restrictions.MaxPrice?.Amount,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var maxPrice)
                                ? maxPrice
                                : null;


                        var listPoint =
                            listData.DeliveryPoints
                                .FirstOrDefault(
                                    x =>
                                        x.DeliveryPointId ==
                                        point.DeliveryPointId);


                        dbPoint.ShipmentMethodId =
                            listPoint?
                                .ShipmentMethodIds
                                .FirstOrDefault();
                    }


                    await _db.SaveChangesAsync();


                    total += infoData
                        .DeliveryPoints
                        .Count;


                    Console.WriteLine(
                        $"БД: сохранено {total}");
                }


                // ==========================================
                // 5. Следующая страница
                // ==========================================

                if (string.IsNullOrWhiteSpace(
                    listData.NextCursor))
                {
                    break;
                }


                cursor =
                    listData.NextCursor;
            }


            // ==========================================
            // ГОТОВО
            // ==========================================

            var databaseCount =
                await _db.OzonDeliveryPoints.CountAsync();


            Console.WriteLine(
                "=================================");
            
            Console.WriteLine(
                "СИНХРОНИЗАЦИЯ ЗАВЕРШЕНА");

            Console.WriteLine(
                $"ПОЛУЧЕНО: {total}");

            Console.WriteLine(
                $"В БД: {databaseCount}");

            Console.WriteLine(
                "=================================");


            return Ok(new
            {
                message =
                    "Синхронизация ПВЗ завершена",

                received = total,

                database_count =
                    databaseCount
            });
        }
    }
}