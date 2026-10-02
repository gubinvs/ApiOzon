using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ApiOzon
{
    /// <summary>
    /// Контроллер принимает методом POST пароль и запускает процесс обновления базы данных 
    /// по точкам ПВЗ ОЗОН обновляя данные или дополняя
    /// </summary>
    /// 
    
    [ApiController]
    [Route("v1/[controller]")]
    public class DeliveryPointSyncController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PasswordGuid _password;
        private readonly ShopDbContext _db;

        // Минимальная задержка между запросами к Ozon
        private static readonly TimeSpan OzonRequestDelay = TimeSpan.FromMilliseconds(1500);

        // Максимальное количество повторов при 
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
            // Проверяем пароль
            // ======================================================

            if (_password.Password != password)
            {
                return Ok(new
                {
                    message = "Пароль не верен!"
                });
            }

            var client = _httpClientFactory.CreateClient("OzonDeliveryClient");
            string? cursor = null;
            int page = 0;
            int total = 0;

            try
            {
                // ==================================================
                // ЦИКЛ ПО СТРАНИЦАМ
                // ==================================================

                while (true)
                {
                    page++;

                    // ==============================================
                    // 1. Формируем запрос списка ПВЗ
                    // ==============================================

                    string requestJson;

                    if (cursor == null)
                    {
                        // ==========================================
                        // ПЕРВАЯ СТРАНИЦА
                        // ==========================================

                        var requestObject = new
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

                        requestJson = JsonSerializer.Serialize(requestObject);
                    }
                    else
                    {
                        // ==========================================
                        // ПОСЛЕДУЮЩИЕ СТРАНИЦЫ
                        // ==========================================

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

                        requestJson = JsonSerializer.Serialize(requestObject);
                    }

                    Console.WriteLine();
                    Console.WriteLine("========================================");
                    Console.WriteLine($"OZON: получаем страницу {page}");
                    Console.WriteLine($"CURSOR: {cursor ?? "NULL"}");
                    Console.WriteLine("========================================");

                    var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    // ==============================================
                    // Запрос к Ozon
                    // ==============================================

                    var response = await PostToOzonAsync(client,"v1/delivery-point/list",content);

                    var responseJson = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return new ContentResult
                        {
                            StatusCode = (int)response.StatusCode, ContentType = "application/json",
                            Content = responseJson
                        };
                    }

                    // ==============================================
                    // Разбираем ответ
                    // ==============================================

                    var listData = JsonSerializer.Deserialize<DeliveryPointListResponse>(responseJson);

                    if (listData == null)
                    {
                        return BadRequest("Не удалось разобрать ответ Ozon");
                    }

                    Console.WriteLine( $"ПВЗ получено: " + $"{listData.DeliveryPoints.Count}");

                    // ==============================================
                    // 2. Получаем ID ПВЗ
                    // ==============================================

                    var ids = listData.DeliveryPoints
                                .Select(x => x.DeliveryPointId)
                                .Distinct()
                                .ToList();

                    Console.WriteLine($"Уникальных ID: {ids.Count}");

                    // ==============================================
                    // 3. Получаем подробную информацию
                    // ==============================================

                    for (int i = 0; i < ids.Count; i += 100)
                    {
                        var batchIds = ids.Skip(i).Take(100).ToList();

                        //Console.WriteLine($"OZON INFO: " + $"запрос {batchIds.Count} ПВЗ");

                        // var infoRequest = new
                        // {
                        //     delivery_point_ids = batchIds
                        // };

                        // var infoJson = JsonSerializer.Serialize(infoRequest);
                        // var infoContent = new StringContent(infoJson, Encoding.UTF8, "application/json");

                        // ==========================================
                        // Запрашиваем подробную информацию
                        // ==========================================

                        // var infoResponse = await PostToOzonAsync(client, "v1/delivery-point/info", infoContent);
                        // var infoResponseJson = await infoResponse.Content.ReadAsStringAsync();

                        // if (!infoResponse.IsSuccessStatusCode)
                        // {
                        //     return new ContentResult
                        //     {
                        //         StatusCode = (int)infoResponse.StatusCode,
                        //         ContentType = "application/json",
                        //         Content = infoResponseJson
                        //     };
                        // }

                        // ==========================================
                        // Разбираем ответ
                        // ==========================================

                        //var infoData = JsonSerializer.Deserialize<DeliveryPointInfoResponse>(infoResponseJson);

                        // if (infoData == null)
                        // {
                        //     Console.WriteLine("OZON: пустой ответ info");
                        //     continue;
                        // }

                        // ==========================================
                        // 4. Сохраняем / обновляем БД
                        // ==========================================
                        foreach (var item in listData.DeliveryPoints)
                        {
                            // ======================================
                            // Ищем запись по DeliveryPointId
                            // ======================================
                            var dbPoint = await _db.OzonDeliveryPoints
                                .FirstOrDefaultAsync(x => x.DeliveryPointId == item.DeliveryPointId);

                            if (dbPoint == null)
                            {
                                // ==================================
                                // НЕТ записи СОЗДАЁМ новую
                                // ==================================
                                dbPoint = new OzonDeliveryPointDb
                                {
                                    DeliveryPointId = item.DeliveryPointId,
                                    DeliveryPointNumber = item.DeliveryPointNumber,
                                    Name = item.Name,
                                    Address = item.FullAddress,
                                    Latitude = item.Latitude,
                                    Longitude = item.Longitude,
                                    IsActive = item.IsActive,
                                   
                                };

                                _db.OzonDeliveryPoints.Add(dbPoint);
                                Console.WriteLine($"EFCORE: ДОБАВЛЕН {item.DeliveryPointId}");
                            }
                            else
                            {
                                // ==================================
                                // ЗАПИСЬ ЕСТЬ ОБНОВЛЯЕМ
                                // ==================================
                                dbPoint.DeliveryPointNumber = item.DeliveryPointNumber;
                                dbPoint.Name = item.Name;
                                dbPoint.Address = item.FullAddress;
                                dbPoint.Latitude = item.Latitude;
                                dbPoint.Longitude = item.Longitude;
                                dbPoint.IsActive = item.IsActive;                   

                                Console.WriteLine($"EFCORE: ОБНОВЛЁН {item.DeliveryPointId}");
                            }
                        }

                        // ==========================================
                        // Сохраняем изменения в БД
                        // ==========================================

                        await _db.SaveChangesAsync();

                        total += listData.DeliveryPoints.Count;

                        Console.WriteLine($"БД: обработано всего {total}");
                    }

                    // ==============================================
                    // 5. Проверяем следующую страницу
                    // ==============================================

                    if (string.IsNullOrWhiteSpace(listData.NextCursor))
                    {
                        Console.WriteLine();
                        Console.WriteLine("OZON: страниц больше нет");

                        break;
                    }

                    cursor = listData.NextCursor;

                    Console.WriteLine($"OZON: следующий cursor = {cursor}");
                }

                // ==================================================
                // ГОТОВО
                // ==================================================

                var databaseCount = await _db.OzonDeliveryPoints.CountAsync();

                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine("СИНХРОНИЗАЦИЯ ЗАВЕРШЕНА");
                Console.WriteLine($"ПОЛУЧЕНО: {total}");
                Console.WriteLine($"В БД: {databaseCount}");
                Console.WriteLine("========================================");

                return Ok(new{message ="Синхронизация ПВЗ завершена", received = total, database_count = databaseCount});
            }
            catch (Exception ex)
            {
                Console.WriteLine("ОШИБКА СИНХРОНИЗАЦИИ:");
                Console.WriteLine(ex.ToString());

                return StatusCode(500,new{message ="Ошибка синхронизации", error = ex.Message});
            }
        }

        // ==========================================================
        // ЗАПРОС К OZON С ЗАДЕРЖКОЙ И ОБРАБОТКОЙ 429
        // ==========================================================

        private async Task<HttpResponseMessage>
        PostToOzonAsync(HttpClient client, string url, HttpContent content)
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                // ==============================================
                // Задержка перед каждым запросом
                // ==============================================

                await Task.Delay(OzonRequestDelay);

                Console.WriteLine($"OZON REQUEST: {url}");
                Console.WriteLine($"Попытка: {attempt}/{MaxRetryAttempts}");
               
                var response = await client.PostAsync(url, content);

                // ==============================================
                // Успешный запрос
                // ==============================================

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                // ==============================================
                // Ozon ограничил количество запросов
                // ==============================================

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    response.Dispose();

                    // 2, 4, 8, 16, 30 секунд
                    var delaySeconds = Math.Min(30, (int)Math.Pow(2,attempt));

                    Console.WriteLine("========================================");
                    Console.WriteLine("OZON: получен HTTP 429");
                    Console.WriteLine($"Повтор через " + $"{delaySeconds} сек.");
                    Console.WriteLine("========================================");

                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    continue;
                }


                // ==============================================
                // Другая ошибка
                // ==============================================

                return response;
            }

            // Теоретически сюда не должны попасть,
            // но оставляем защиту
            throw new Exception("Ozon не отвечает после " +$"{MaxRetryAttempts} попыток.");
        }
    }
}
