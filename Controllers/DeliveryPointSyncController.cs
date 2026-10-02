using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ApiOzon.Services
{
    public class OzonDeliverySyncService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ShopDbContext _db;

        private static readonly TimeSpan NormalDelay =
            TimeSpan.FromSeconds(3);

        private static readonly TimeSpan MaxRetryDelay =
            TimeSpan.FromHours(1);

        private const int MaxRetryAttempts = 10;

        public OzonDeliverySyncService(
            IHttpClientFactory httpClientFactory,
            ShopDbContext db)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
        }

        public async Task RunAsync(
            CancellationToken cancellationToken)
        {
            var state =
                await _db.OzonDeliverySyncStates
                    .FirstOrDefaultAsync(
                        x => x.Id == 1,
                        cancellationToken);

            if (state == null)
            {
                state = new OzonDeliverySyncState
                {
                    Id = 1,
                    Page = 0,
                    IsRunning = false
                };

                _db.OzonDeliverySyncStates.Add(state);

                await _db.SaveChangesAsync(
                    cancellationToken);
            }

            // ======================================================
            // НЕ ЗАПУСКАЕМ ВТОРУЮ СИНХРОНИЗАЦИЮ
            // ======================================================

            if (state.IsRunning)
            {
                Console.WriteLine(
                    "OZON: синхронизация уже запущена.");

                return;
            }

            state.IsRunning = true;
            state.StartedAt = DateTime.UtcNow;
            state.FinishedAt = null;
            state.LastError = null;

            await _db.SaveChangesAsync(
                cancellationToken);

            try
            {
                await SyncInternalAsync(
                    state,
                    cancellationToken);

                state.IsRunning = false;
                state.FinishedAt = DateTime.UtcNow;
                state.LastSuccessAt = DateTime.UtcNow;
                state.LastError = null;

                await _db.SaveChangesAsync(
                    cancellationToken);

                Console.WriteLine();
                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
                    "OZON: СИНХРОНИЗАЦИЯ ЗАВЕРШЕНА");

                Console.WriteLine(
                    $"Получено: {state.TotalReceived}");

                Console.WriteLine(
                    $"Добавлено: {state.TotalAdded}");

                Console.WriteLine(
                    $"Обновлено: {state.TotalUpdated}");

                Console.WriteLine(
                    $"Пропущено: {state.TotalSkipped}");
            }
            catch (OperationCanceledException)
            {
                state.IsRunning = false;

                await _db.SaveChangesAsync(
                    CancellationToken.None);

                Console.WriteLine(
                    "OZON: синхронизация остановлена.");
            }
            catch (Exception ex)
            {
                state.IsRunning = false;
                state.LastError = ex.ToString();

                await _db.SaveChangesAsync(
                    CancellationToken.None);

                Console.WriteLine();
                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
                    "OZON: ОШИБКА СИНХРОНИЗАЦИИ");

                Console.WriteLine(ex);
            }
        }

        // ==========================================================
        // ОСНОВНАЯ СИНХРОНИЗАЦИЯ
        // ==========================================================

        private async Task SyncInternalAsync(
            OzonDeliverySyncState state,
            CancellationToken cancellationToken)
        {
            var client =
                _httpClientFactory.CreateClient(
                    "OzonDeliveryClient");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                state.Page++;

                Console.WriteLine();
                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
                    $"OZON: страница {state.Page}");

                Console.WriteLine(
                    $"OZON: cursor = " +
                    $"{state.Cursor ?? "NULL"}");

                // ==================================================
                // LIST
                // ==================================================

                object requestObject;

                if (string.IsNullOrWhiteSpace(
                    state.Cursor))
                {
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
                    requestObject = new
                    {
                        type = new[]
                        {
                            "1"
                        },

                        pagination = new
                        {
                            limit = 100,
                            cursor = state.Cursor
                        }
                    };
                }

                var requestJson =
                    JsonSerializer.Serialize(
                        requestObject);

                var listResponse =
                    await PostToOzonAsync(
                        client,
                        "v1/delivery-point/list",
                        requestJson,
                        cancellationToken);

                var listJson =
                    await listResponse.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                if (!listResponse.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Ozon LIST HTTP " +
                        $"{(int)listResponse.StatusCode}: " +
                        listJson);
                }

                var listData =
                    JsonSerializer.Deserialize<
                        DeliveryPointListResponse>(
                            listJson);

                if (listData == null)
                {
                    throw new Exception(
                        "Ozon LIST: пустой ответ.");
                }

                if (listData.DeliveryPoints.Count == 0)
                {
                    Console.WriteLine(
                        "OZON: список ПВЗ пуст.");

                    break;
                }

                state.TotalReceived +=
                    listData.DeliveryPoints.Count;

                Console.WriteLine(
                    $"OZON: получено " +
                    $"{listData.DeliveryPoints.Count} ПВЗ");

                // ==================================================
                // INFO
                // ==================================================

                var deliveryPointIds =
                    listData.DeliveryPoints
                        .Select(x =>
                            x.DeliveryPointId)
                        .Distinct()
                        .ToList();

                var infoRequest =
                    new DeliveryPointInfoRequest
                    {
                        DeliveryPointIds =
                            deliveryPointIds
                    };

                var infoJson =
                    JsonSerializer.Serialize(
                        infoRequest);

                var infoResponse =
                    await PostToOzonAsync(
                        client,
                        "v1/delivery-point/info",
                        infoJson,
                        cancellationToken);

                var infoResponseJson =
                    await infoResponse.Content
                        .ReadAsStringAsync(
                            cancellationToken);

                if (!infoResponse.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Ozon INFO HTTP " +
                        $"{(int)infoResponse.StatusCode}: " +
                        infoResponseJson);
                }

                var infoData =
                    JsonSerializer.Deserialize<
                        DeliveryPointInfoResponse>(
                            infoResponseJson);

                if (infoData == null)
                {
                    throw new Exception(
                        "Ozon INFO: пустой ответ.");
                }

                // ==================================================
                // СОХРАНЯЕМ ПВЗ
                // ==================================================

                foreach (
                    var listPoint
                    in listData.DeliveryPoints)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

                    var infoPoint =
                        infoData.DeliveryPoints
                            .FirstOrDefault(x =>
                                x.DeliveryPointId ==
                                listPoint.DeliveryPointId);

                    if (infoPoint == null)
                    {
                        Console.WriteLine(
                            $"OZON INFO: не найден " +
                            $"ID={listPoint.DeliveryPointId}");

                        state.TotalSkipped++;

                        continue;
                    }

                    var dbPoint =
                        await _db.OzonDeliveryPoints
                            .FirstOrDefaultAsync(
                                x =>
                                    x.DeliveryPointId ==
                                    infoPoint.DeliveryPointId,
                                cancellationToken);

                    if (dbPoint == null)
                    {
                        dbPoint =
                            new OzonDeliveryPointDb
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

                        _db.OzonDeliveryPoints.Add(
                            dbPoint);

                        state.TotalAdded++;
                    }
                    else
                    {
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

                        state.TotalUpdated++;
                    }
                }

                // ==================================================
                // ВАЖНО:
                //
                // СНАЧАЛА сохраняем ПВЗ.
                // ==================================================

                await _db.SaveChangesAsync(
                    cancellationToken);

                // ==================================================
                // И ТОЛЬКО ПОСЛЕ ЭТОГО сохраняем cursor.
                // ==================================================

                state.Cursor =
                    listData.NextCursor;

                await _db.SaveChangesAsync(
                    cancellationToken);

                Console.WriteLine(
                    $"OZON: страница {state.Page} сохранена.");

                Console.WriteLine(
                    $"OZON: cursor сохранён.");

                // ==================================================
                // ПОСЛЕДНЯЯ СТРАНИЦА
                // ==================================================

                if (string.IsNullOrWhiteSpace(
                    state.Cursor))
                {
                    Console.WriteLine(
                        "OZON: страниц больше нет.");

                    break;
                }

                // ==================================================
                // ПАУЗА
                // ==================================================

                Console.WriteLine(
                    "OZON: ждём 3 секунды...");

                await Task.Delay(
                    NormalDelay,
                    cancellationToken);
            }
        }

        // ==========================================================
        // HTTP REQUEST + RETRY
        // ==========================================================

        private async Task<HttpResponseMessage>
            PostToOzonAsync(
                HttpClient client,
                string url,
                string json,
                CancellationToken cancellationToken)
        {
            for (
                int attempt = 1;
                attempt <= MaxRetryAttempts;
                attempt++)
            {
                TimeSpan retryDelay;

                if (attempt == 1)
                {
                    retryDelay =
                        TimeSpan.Zero;
                }
                else
                {
                    // 1 мин
                    // 2 мин
                    // 4 мин
                    // 8 мин
                    // 16 мин
                    // 32 мин
                    // 60 мин
                    // 60 мин...

                    var seconds =
                        Math.Pow(
                            2,
                            attempt - 2)
                        * 60;

                    retryDelay =
                        TimeSpan.FromSeconds(
                            Math.Min(
                                seconds,
                                MaxRetryDelay
                                    .TotalSeconds));
                }

                if (retryDelay > TimeSpan.Zero)
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        $"OZON: следующая попытка " +
                        $"через {retryDelay}");

                    await Task.Delay(
                        retryDelay,
                        cancellationToken);
                }

                try
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        $"OZON REQUEST: {url}");

                    Console.WriteLine(
                        $"Попытка " +
                        $"{attempt}/{MaxRetryAttempts}");

                    using var content =
                        new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json");

                    var response =
                        await client.PostAsync(
                            url,
                            content,
                            cancellationToken);

                    // ==================================================
                    // УСПЕХ
                    // ==================================================

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(
                            $"OZON: HTTP " +
                            $"{(int)response.StatusCode}");

                        return response;
                    }

                    // ==================================================
                    // 429
                    // ==================================================

                    if (response.StatusCode ==
                        HttpStatusCode.TooManyRequests)
                    {
                        Console.WriteLine(
                            "OZON: HTTP 429.");

                        response.Dispose();

                        continue;
                    }

                    // ==================================================
                    // 5XX
                    // ==================================================

                    if ((int)response.StatusCode >= 500)
                    {
                        Console.WriteLine(
                            $"OZON: HTTP " +
                            $"{(int)response.StatusCode}");

                        response.Dispose();

                        continue;
                    }

                    // ==================================================
                    // Остальные 4XX
                    //
                    // Их повторять не будем.
                    // ==================================================

                    return response;
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        "========================================");

                    Console.WriteLine(
                        "OZON: СЕТЕВАЯ ОШИБКА");

                    Console.WriteLine(
                        $"Попытка: " +
                        $"{attempt}/{MaxRetryAttempts}");

                    Console.WriteLine(
                        $"Ошибка: {ex.Message}");

                    if (ex.InnerException != null)
                    {
                        Console.WriteLine(
                            $"Inner: " +
                            $"{ex.InnerException.Message}");
                    }

                    if (attempt ==
                        MaxRetryAttempts)
                    {
                        throw;
                    }

                    Console.WriteLine(
                        "OZON: запрос будет повторен.");
                }
            }

            throw new Exception(
                "Ozon не отвечает после " +
                $"{MaxRetryAttempts} попыток.");
        }
    }
}
