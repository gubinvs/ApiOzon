using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ApiOzon
{
    /// <summary>
    /// Синхронизация ПВЗ Ozon с локальной базой данных.
    ///
    /// Алгоритм:
    /// 1. Получаем страницу ПВЗ через /v1/delivery-point/list
    /// 2. Получаем подробную информацию через /v1/delivery-point/info
    /// 3. Объединяем данные по DeliveryPointId
    /// 4. Добавляем или обновляем записи в БД
    /// 5. Переходим на следующую страницу по cursor
    /// </summary>
    [ApiController]
    [Route("v1/[controller]")]
    public class DeliveryPointSyncController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PasswordGuid _password;
        private readonly ShopDbContext _db;

        // Минимальная задержка между запросами к Ozon
        private static readonly TimeSpan OzonRequestDelay =
            TimeSpan.FromMilliseconds(1500);

        // Максимальное количество попыток запроса
        private const int MaxRetryAttempts = 5;

        public DeliveryPointSyncController(
            IHttpClientFactory httpClientFactory,
            IOptions<PasswordGuid> options,
            ShopDbContext db)
        {
            _httpClientFactory = httpClientFactory;
            _password = options.Value;
            _db = db;
        }

        // ==========================================================
        // ОСНОВНАЯ СИНХРОНИЗАЦИЯ
        // ==========================================================

        [HttpPost]
        public async Task<IActionResult> Sync(string password)
        {
            // ======================================================
            // 1. ПРОВЕРКА ПАРОЛЯ
            // ======================================================

            if (_password.Password != password)
            {
                return Ok(new
                {
                    message = "Пароль не верен!"
                });
            }

            var client = _httpClientFactory
                .CreateClient("OzonDeliveryClient");

            string? cursor = null;

            int page = 0;
            int totalReceived = 0;
            int totalAdded = 0;
            int totalUpdated = 0;
            int totalSkipped = 0;

            try
            {
                // ==================================================
                // ЦИКЛ ПО СТРАНИЦАМ
                // ==================================================

                while (true)
                {
                    page++;

                    // ==================================================
                    // 2. ФОРМИРУЕМ ЗАПРОС LIST
                    // ==================================================

                    object requestObject;

                    if (cursor == null)
                    {
                        // Первая страница
                        requestObject = new
                        {
                            type = new[]
                            {
                                "1"
                            },

                            pagination = new
                            {
                                offset = 0,
                                limit = 100
                            }
                        };
                    }
                    else
                    {
                        // Следующая страница
                        requestObject = new
                        {
                            type = new[]
                            {
                                "1"
                            },

                            pagination = new
                            {
                                limit = 100,
                                cursor = cursor
                            }
                        };
                    }

                    var requestJson =
                        JsonSerializer.Serialize(requestObject);

                    var content = new StringContent(
                        requestJson,
                        Encoding.UTF8,
                        "application/json");

                    // ==================================================
                    // 3. LIST
                    // ==================================================

                    var listResponse = await PostToOzonAsync(
                        client,
                        "v1/delivery-point/list",
                        content);

                    var listJson =
                        await listResponse.Content.ReadAsStringAsync();

                    if (!listResponse.IsSuccessStatusCode)
                    {
                        return new ContentResult
                        {
                            StatusCode = (int)listResponse.StatusCode,
                            ContentType = "application/json",
                            Content = listJson
                        };
                    }

                    var listData =
                        JsonSerializer.Deserialize<DeliveryPointListResponse>(
                            listJson);

                    if (listData == null)
                    {
                        return BadRequest(
                            "Не удалось разобрать ответ Ozon /list");
                    }


                    // Если страница пустая — заканчиваем
                    if (listData.DeliveryPoints.Count == 0)
                    {

                        break;
                    }

                    totalReceived +=
                        listData.DeliveryPoints.Count;

                    // ==================================================
                    // 4. ПОЛУЧАЕМ ID ПВЗ
                    // ==================================================

                    var deliveryPointIds =
                        listData.DeliveryPoints
                            .Select(x => x.DeliveryPointId)
                            .Distinct()
                            .ToList();


                    // ==================================================
                    // 5. INFO
                    // ==================================================

                    var infoRequest =
                        new DeliveryPointInfoRequest
                        {
                            DeliveryPointIds = deliveryPointIds
                        };

                    var infoJson =
                        JsonSerializer.Serialize(infoRequest);

                    var infoContent = new StringContent(
                        infoJson,
                        Encoding.UTF8,
                        "application/json");

                    var infoResponse = await PostToOzonAsync(
                        client,
                        "v1/delivery-point/info",
                        infoContent);

                    var infoResponseJson =
                        await infoResponse.Content.ReadAsStringAsync();

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
                        Console.WriteLine(
                            "OZON INFO: пустой ответ");

                        totalSkipped +=
                            listData.DeliveryPoints.Count;

                        cursor = listData.NextCursor;

                        if (string.IsNullOrWhiteSpace(cursor))
                            break;

                        continue;
                    }


                    // ==================================================
                    // 6. СОХРАНЯЕМ / ОБНОВЛЯЕМ БД
                    // ==================================================

                    foreach (var listPoint in listData.DeliveryPoints)
                    {
                        var infoPoint =
                            infoData.DeliveryPoints
                                .FirstOrDefault(x =>
                                    x.DeliveryPointId ==
                                    listPoint.DeliveryPointId);

                        // INFO не вернул этот ПВЗ
                        if (infoPoint == null)
                        {
                            Console.WriteLine(
                                $"OZON INFO: не найден ID=" +
                                $"{listPoint.DeliveryPointId}");

                            totalSkipped++;

                            continue;
                        }


                        // Ищем существующую запись
                        var dbPoint =
                            await _db.OzonDeliveryPoints
                                .FirstOrDefaultAsync(x =>
                                    x.DeliveryPointId ==
                                    infoPoint.DeliveryPointId);

                        if (dbPoint == null)
                        {
                            // ==========================================
                            // ДОБАВЛЯЕМ
                            // ==========================================

                            dbPoint = new OzonDeliveryPointDb
                            {
                                DeliveryPointId =
                                    infoPoint.DeliveryPointId,

                                DeliveryPointNumber =
                                    infoPoint.DeliveryPointNumber,

                                Name =
                                    infoPoint.Name,

                                Address =
                                    infoPoint.FullAddress,

                                Latitude =
                                    infoPoint.Coordinates.Latitude,

                                Longitude =
                                    infoPoint.Coordinates.Longitude,

                                IsActive =
                                    infoPoint.IsActive
                            };

                            _db.OzonDeliveryPoints.Add(dbPoint);

                            totalAdded++;
                        }
                        else
                        {
                            // ==========================================
                            // ОБНОВЛЯЕМ
                            // ==========================================

                            dbPoint.DeliveryPointNumber =
                                infoPoint.DeliveryPointNumber;

                            dbPoint.Name =
                                infoPoint.Name;

                            dbPoint.Address =
                                infoPoint.FullAddress;

                            dbPoint.Latitude =
                                infoPoint.Coordinates.Latitude;

                            dbPoint.Longitude =
                                infoPoint.Coordinates.Longitude;

                            dbPoint.IsActive =
                                infoPoint.IsActive;

                            totalUpdated++;

                        }
                    }

                    // ==================================================
                    // 7. СОХРАНЯЕМ ИЗМЕНЕНИЯ
                    // ==================================================

                    await _db.SaveChangesAsync();

                    // ==================================================
                    // 8. СЛЕДУЮЩАЯ СТРАНИЦА
                    // ==================================================

                    if (string.IsNullOrWhiteSpace(
                        listData.NextCursor))
                    {
                        Console.WriteLine();
                        Console.WriteLine(
                            "OZON: страниц больше нет");

                        break;
                    }

                    cursor = listData.NextCursor;

                    Console.WriteLine(
                        $"OZON: следующий cursor = {cursor}");
                }

                // ======================================================
                // 9. ИТОГ
                // ======================================================

                var databaseCount =
                    await _db.OzonDeliveryPoints.CountAsync();


                return Ok(new
                {
                    message =
                        "Синхронизация ПВЗ завершена",

                    pages = page,

                    received = totalReceived,

                    added = totalAdded,

                    updated = totalUpdated,

                    skipped = totalSkipped,

                    database_count = databaseCount
                });
            }
            catch (Exception ex)
            {

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "Ошибка синхронизации",

                        error =
                            ex.Message
                    });
            }
        }

        // ==========================================================
        // ЗАПРОС К OZON
        // ==========================================================

        private async Task<HttpResponseMessage> PostToOzonAsync(
            HttpClient client,
            string url,
            HttpContent content)
        {
            for (
                int attempt = 1;
                attempt <= MaxRetryAttempts;
                attempt++)
            {
                // ==============================================
                // Задержка перед запросом
                // ==============================================

                await Task.Delay(OzonRequestDelay);

                var response =
                    await client.PostAsync(
                        url,
                        content);

                // ==============================================
                // Успешный запрос
                // ==============================================

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                // ==============================================
                // HTTP 429
                // ==============================================

                if (response.StatusCode ==
                    HttpStatusCode.TooManyRequests)
                {
                    response.Dispose();

                    var delaySeconds =
                        Math.Min(
                            30,
                            (int)Math.Pow(2, attempt));


                    await Task.Delay(
                        TimeSpan.FromSeconds(
                            delaySeconds));

                    continue;
                }

                // ==============================================
                // Другая ошибка
                // ==============================================

                return response;
            }

            throw new Exception(
                "Ozon не отвечает после " +
                $"{MaxRetryAttempts} попыток.");
        }
    }
}
