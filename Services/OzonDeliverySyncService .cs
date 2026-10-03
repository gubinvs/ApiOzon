using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ApiOzon
{
    public class OzonDeliverySyncService 
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ShopDbContext _db;

        // Время ожидания перед повтором, если упала сеть Озона
        private static readonly TimeSpan ReconnectDelay = TimeSpan.FromHours(1); 

        public OzonDeliverySyncService(IHttpClientFactory httpClientFactory, ShopDbContext db)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var state = await _db.OzonDeliverySyncStates.FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);

            if (state == null)
            {
                state = new OzonDeliverySyncState { Id = 1, Page = 0, IsRunning = false };
                _db.OzonDeliverySyncStates.Add(state);
                await _db.SaveChangesAsync(cancellationToken);
            }
            
            if (state.IsRunning)
            {
                Console.WriteLine("OZON: Обнаружен незавершенный сеанс после сбоя. Сбрасываю флаг и восстанавливаю работу...");
                
                // Вместо return; мы принудительно даем воркеру продолжить работу
                state.IsRunning = false; 
                await _db.SaveChangesAsync(cancellationToken);
            }

            state.IsRunning = true;
            state.StartedAt = DateTime.UtcNow;
            state.FinishedAt = null;
            state.LastError = null;
            await _db.SaveChangesAsync(cancellationToken);

            // Главный цикл жизнеобеспечения службы (работает до победного конца или отмены)
                        // Главный цикл жизнеобеспечения службы (работает до победного конца или отмены)
            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Запуск внутреннего цикла страниц
                    bool isFullyFinished = await SyncInternalAsync(state, cancellationToken);

                    // Если метод вернул true, значит Озон отдал всё до последней страницы
                    if (isFullyFinished)
                    {
                        state.IsRunning = false;
                        state.FinishedAt = DateTime.UtcNow;
                        state.LastSuccessAt = DateTime.UtcNow;
                        state.LastError = null;
                        
                        try
                        {
                            await _db.SaveChangesAsync(CancellationToken.None);
                        }
                        catch (Exception dbEx)
                        {
                            Console.WriteLine($"[!] Предупреждение: Не удалось сохранить финальный статус в БД: {dbEx.Message}");
                        }

                        Console.WriteLine("\n========================================");
                        Console.WriteLine("OZON: СИНХРОНИЗАЦИЯ УСПЕШНО ЗАВЕРШЕНА");
                        Console.WriteLine($"Получено: {state.TotalReceived} ПВЗ");
                        
                        break; 
                    }
                }
                catch (OperationCanceledException)
                {
                    state.IsRunning = false;
                    try { await _db.SaveChangesAsync(CancellationToken.None); } catch { }
                        Console.WriteLine("OZON: синхронизация принудительно остановлена.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n========================================");
                    Console.WriteLine($"OZON: ОБРЫВ СВЯЗИ (Сеть/БД). Следующая попытка через 1 час.");
                    Console.WriteLine($"Детали ошибки: {ex.Message}");

                    // БЕЗОПАСНО очищаем кэш EF Core от недосохраненных ПВЗ, чтобы они не дублировались через час
                    try
                    {
                        _db.ChangeTracker.Clear(); 
                    }
                    catch { }

                    try
                    {
                        state.LastError = $"[Сбой связи. Ожидание 1 час]: {ex.Message}";
                        await _db.SaveChangesAsync(CancellationToken.None);
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"[!] База данных недоступна. Ошибка не записана в БД: {dbEx.Message}");
                    }

                    // Спокойно спим час...
                    await Task.Delay(ReconnectDelay, cancellationToken);
                }
            }
        }

        // Возвращает TRUE если все страницы скачаны, или FALSE/Исключение если связь оборвалась
        private async Task<bool> SyncInternalAsync(OzonDeliverySyncState state, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("OzonDeliveryClient");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                state.Page++;
                Console.WriteLine($"\nOZON: страница {state.Page} (cursor = {state.Cursor ?? "NULL"})");

                object requestObject = string.IsNullOrWhiteSpace(state.Cursor) 
                    ? new { type = new[] { "1" }, pagination = new { offset = 0, limit = 100 } }
                    : new { type = new[] { "1" }, pagination = new { limit = 100, cursor = state.Cursor } };

                var requestJson = JsonSerializer.Serialize(requestObject);
                
                // Делаем обычный сетевой вызов для получения списка ПВЗ
                var listResponse = await PostToOzonAsync(client, "v1/delivery-point/list", requestJson, cancellationToken);
                var listJson = await listResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!listResponse.IsSuccessStatusCode)
                {
                    // Падаем в catch внешнего метода, чтобы уйти на часовое ожидание
                    throw new Exception($"Ozon HTTP {(int)listResponse.StatusCode}: {listJson}"); 
                }

                var listData = JsonSerializer.Deserialize<DeliveryPointListResponse>(listJson);
                if (listData == null) throw new Exception("Ozon LIST: пустой ответ.");

                // Условие выхода — Озон вернул пустой список (данные кончились)
                if (listData.DeliveryPoints == null || listData.DeliveryPoints.Count == 0)
                {
                    Console.WriteLine("OZON: список ПВЗ пуст. Достигнут конец данных.");
                    return true; 
                }

                // ==========================================
                // 2. Получаем ID ПВЗ
                // ==========================================

                var deliveryPointIds = listData
                    .DeliveryPoints
                    .Select(x => x.DeliveryPointId)
                    .Distinct()
                    .ToList();

                // ==========================================
                // 3. Запрашиваем подробную информацию
                // ==========================================

                var infoRequest = new DeliveryPointInfoRequest
                {
                    DeliveryPointIds = deliveryPointIds
                };

                var infoResponse = await client.PostAsJsonAsync("v1/delivery-point/info",infoRequest);
                var infoJson = await infoResponse.Content.ReadAsStringAsync();

                var infoData = JsonSerializer.Deserialize<DeliveryPointInfoResponse>(infoJson);

                if (infoData == null)
                {
                    Console.WriteLine("OZON: Информации о ПВЗ нет. Достигнут конец данных.");
                    return true; 
                }


                state.TotalReceived += listData.DeliveryPoints.Count;

                // ==================================================
                // СОХРАНЕНИЕ ДАННЫХ В БАЗУ ДАННЫХ (OzonDeliveryPoints)
                // ==================================================
                foreach (var pointDto in infoData.DeliveryPoints)
                {
                    // Ищем ПВЗ в БД по его уникальному идентификатору от Ozon
                    var existingPoint = await _db.OzonDeliveryPoints
                        .FirstOrDefaultAsync(x => x.DeliveryPointId == pointDto.DeliveryPointId, cancellationToken);

                    if (existingPoint != null)
                    {
                        // Маппинг полей для ОБНОВЛЕНИЯ существующего ПВЗ
                        existingPoint.DeliveryPointNumber = pointDto.DeliveryPointNumber;
                        existingPoint.Name = pointDto.Name; 
                        existingPoint.Address = pointDto.FullAddress;
                        existingPoint.Latitude = pointDto.Coordinates.Latitude; 
                        existingPoint.Longitude = pointDto.Coordinates.Longitude;
                        existingPoint.IsActive = pointDto.IsActive;
                    }
                    else
                    {
                        // Создание и ДОБАВЛЕНИЕ нового ПВЗ, если его не было в базе
                        var newPoint = new OzonDeliveryPointDb
                        {
                            DeliveryPointId = pointDto.DeliveryPointId,
                            DeliveryPointNumber = pointDto.DeliveryPointNumber,
                            Name = pointDto.Name,
                            Address = pointDto.FullAddress,
                            Latitude = pointDto.Coordinates.Latitude,
                            Longitude = pointDto.Coordinates.Longitude,
                            IsActive = pointDto.IsActive
                        };
                        _db.OzonDeliveryPoints.Add(newPoint);
                    }
                }

                // ==================================================
                // ОБНОВЛЕНИЕ КУРСОРА ДЛЯ СЛЕДУЮЩЕЙ СТРАНИЦЫ
                // ==================================================
                // Проверьте свойство NextCursor в вашем DeliveryPointListResponse
                state.Cursor = listData.NextCursor; 
                
                await _db.SaveChangesAsync(cancellationToken);
                Console.WriteLine($"Успешно обработано {listData.DeliveryPoints.Count} ПВЗ.");
            }
        }

        private async Task<HttpResponseMessage> PostToOzonAsync(HttpClient client, string url, string jsonContent, CancellationToken cancellationToken)
        {
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            return await client.PostAsync(url, content, cancellationToken);
        }
    }
}
