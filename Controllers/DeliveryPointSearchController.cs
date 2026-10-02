using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApiOzon.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class DeliveryPointSearchController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly YandexGeocoderParam _yandexConfig;

        public DeliveryPointSearchController(
            IHttpClientFactory httpClientFactory,
            IOptions<YandexGeocoderParam> yandexConfig)
        {
            _httpClientFactory = httpClientFactory;
            _yandexConfig = yandexConfig.Value;
        }


        [HttpPost]
        public async Task<IActionResult> Search(
            [FromBody] DeliveryPointSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new
                {
                    message = "Не указан адрес для поиска."
                });
            }


            // ---------------------------------------------------------
            // 1. ЯНДЕКС — адрес -> координаты
            // ---------------------------------------------------------

            var coordinates = await GeocodeAddress(request.Query);

            if (coordinates == null)
            {
                return NotFound(new
                {
                    message = "Адрес не найден."
                });
            }


            // ---------------------------------------------------------
            // 2. Ozon — получаем все ID ПВЗ
            // ---------------------------------------------------------

            var client =
                _httpClientFactory.CreateClient("OzonDeliveryClient");


            var allListPoints =
                await GetAllDeliveryPointIds(client);


            if (allListPoints.Count == 0)
            {
                return Ok(new
                {
                    query = request.Query,
                    latitude = coordinates.Latitude,
                    longitude = coordinates.Longitude,
                    delivery_points = new List<object>()
                });
            }


            // ---------------------------------------------------------
            // 3. Получаем информацию о ПВЗ партиями
            // ---------------------------------------------------------

            var allInfoPoints =
                await GetDeliveryPointInfo(
                    client,
                    allListPoints);


            // ---------------------------------------------------------
            // 4. Объединяем list + info
            // ---------------------------------------------------------

            var points = allListPoints
                .Join(
                    allInfoPoints,

                    listPoint =>
                        listPoint.DeliveryPointId,

                    infoPoint =>
                        infoPoint.DeliveryPointId,

                    (listPoint, infoPoint) =>
                    {
                        var distance =
                            CalculateDistanceKm(
                                coordinates.Latitude,
                                coordinates.Longitude,
                                infoPoint.Coordinates.Latitude,
                                infoPoint.Coordinates.Longitude
                            );

                        return new
                        {
                            delivery_point_id =
                                infoPoint.DeliveryPointId,

                            delivery_point_number =
                                infoPoint.DeliveryPointNumber,

                            name =
                                infoPoint.Name,

                            address =
                                infoPoint.FullAddress,

                            lat =
                                infoPoint.Coordinates.Latitude,

                            lng =
                                infoPoint.Coordinates.Longitude,

                            is_active =
                                infoPoint.IsActive,

                            storage_period_days =
                                infoPoint.StoragePeriodDays,

                            fitting_rooms_count =
                                infoPoint.FittingRoomsCount,

                            is_bulky =
                                infoPoint.IsBulky,

                            max_weight_g =
                                infoPoint.Restrictions.MaxWeightG,

                            max_width_mm =
                                infoPoint.Restrictions.MaxWidthMm,

                            max_length_mm =
                                infoPoint.Restrictions.MaxLengthMm,

                            max_height_mm =
                                infoPoint.Restrictions.MaxHeightMm,

                            max_price =
                                infoPoint.Restrictions.MaxPrice,

                            shipment_method_id =
                                listPoint.ShipmentMethodIds
                                    .FirstOrDefault(),

                            distance_km = Math.Round(
                                distance,
                                2)
                        };
                    })
                .Where(x => x.is_active)
                .OrderBy(x => x.distance_km)
                .Take(20)
                .ToList();


            // ---------------------------------------------------------
            // 5. Возвращаем ближайшие 20
            // ---------------------------------------------------------

            return Ok(new
            {
                query = request.Query,

                latitude =
                    coordinates.Latitude,

                longitude =
                    coordinates.Longitude,

                delivery_points = points
            });
        }


        // =============================================================
        // ЯНДЕКС ГЕОКОДЕР
        // =============================================================

        private async Task<GeoCoordinates?> GeocodeAddress(
            string address)
        {
            using var httpClient = new HttpClient();

            var url =
                "https://geocode-maps.yandex.ru/v1/?" +
                "apikey=" +
                Uri.EscapeDataString(_yandexConfig.ApiKey) +
                "&geocode=" +
                Uri.EscapeDataString(address) +
                "&lang=ru_RU" +
                "&results=1" +
                "&format=json";

            var response =
                await httpClient.GetAsync(url);

            var json =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"Yandex Geocoder HTTP {(int)response.StatusCode}: {json}");

                return null;
            }

            try
            {
                using var document =
                    JsonDocument.Parse(json);

                var featureMember =
                    document.RootElement
                        .GetProperty("response")
                        .GetProperty("GeoObjectCollection")
                        .GetProperty("featureMember");

                if (featureMember.GetArrayLength() == 0)
                {
                    Console.WriteLine(
                        $"Яндекс не нашёл адрес: {address}");

                    return null;
                }

                var pos =
                    featureMember[0]
                        .GetProperty("GeoObject")
                        .GetProperty("Point")
                        .GetProperty("pos")
                        .GetString();

                if (string.IsNullOrWhiteSpace(pos))
                {
                    return null;
                }

                Console.WriteLine(
                    $"Yandex address: {address}");

                Console.WriteLine(
                    $"Yandex coordinates: {pos}");

                // Яндекс возвращает:
                //
                // longitude latitude
                //
                // Например:
                // 30.335098 59.934280

                var parts =
                    pos.Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    return null;
                }

                var longitude =
                    double.Parse(
                        parts[0],
                        System.Globalization.CultureInfo.InvariantCulture);

                var latitude =
                    double.Parse(
                        parts[1],
                        System.Globalization.CultureInfo.InvariantCulture);

                return new GeoCoordinates
                {
                    Latitude = latitude,
                    Longitude = longitude
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка разбора ответа Yandex Geocoder: {ex.Message}");

                Console.WriteLine(json);

                return null;
            }
        }

        // =============================================================
        // Ozon LIST
        // =============================================================

        private async Task<List<DeliveryPointListItem>>
            GetAllDeliveryPointIds(
                HttpClient client)
        {
            var result =
                new List<DeliveryPointListItem>();


            const int limit = 100;

            var offset = 0;


            while (true)
            {
                var request =
                    new DeliveryPointListRequest
                    {
                        Type = new List<string>
                        {
                            "pickup"
                        },

                        Pagination =
                            new DeliveryPointPagination
                            {
                                Offset = offset,
                                Limit = limit
                            }
                    };


                var response =
                    await client.PostAsJsonAsync(
                        "v1/delivery-point/list",
                        request);


                if (!response.IsSuccessStatusCode)
                {
                    break;
                }


                var data =
                    await response.Content
                        .ReadFromJsonAsync<
                            DeliveryPointListResponse>();


                if (data == null ||
                    data.DeliveryPoints.Count == 0)
                {
                    break;
                }


                result.AddRange(
                    data.DeliveryPoints);


                if (data.DeliveryPoints.Count < limit)
                {
                    break;
                }


                offset += limit;
            }


            return result
                .GroupBy(x => x.DeliveryPointId)
                .Select(x => x.First())
                .ToList();
        }


        // =============================================================
        // Ozon INFO
        // =============================================================

        private async Task<List<DeliveryPointInfo>>
            GetDeliveryPointInfo(
                HttpClient client,
                List<DeliveryPointListItem> listPoints)
        {
            var result =
                new List<DeliveryPointInfo>();


            // Отправляем ID партиями по 100

            const int batchSize = 100;


            for (
                int i = 0;
                i < listPoints.Count;
                i += batchSize)
            {
                var batch =
                    listPoints
                        .Skip(i)
                        .Take(batchSize)
                        .Select(x =>
                            x.DeliveryPointId)
                        .ToList();


                var request =
                    new DeliveryPointInfoRequest
                    {
                        DeliveryPointIds = batch
                    };


                var response =
                    await client.PostAsJsonAsync(
                        "v1/delivery-point/info",
                        request);


                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }


                var data =
                    await response.Content
                        .ReadFromJsonAsync<
                            DeliveryPointInfoResponse>();


                if (data != null)
                {
                    result.AddRange(
                        data.DeliveryPoints);
                }
            }


            return result
                .GroupBy(x => x.DeliveryPointId)
                .Select(x => x.First())
                .ToList();
        }


        // =============================================================
        // РАССТОЯНИЕ МЕЖДУ ДВУМЯ КООРДИНАТАМИ
        // =============================================================

        private static double CalculateDistanceKm(
            double lat1,
            double lon1,
            double lat2,
            double lon2)
        {
            const double earthRadius = 6371.0;


            var dLat =
                DegreesToRadians(
                    lat2 - lat1);

            var dLon =
                DegreesToRadians(
                    lon2 - lon1);


            var a =
                Math.Sin(dLat / 2) *
                Math.Sin(dLat / 2) +

                Math.Cos(
                    DegreesToRadians(lat1)) *

                Math.Cos(
                    DegreesToRadians(lat2)) *

                Math.Sin(dLon / 2) *
                Math.Sin(dLon / 2);


            var c =
                2 *
                Math.Atan2(
                    Math.Sqrt(a),
                    Math.Sqrt(1 - a));


            return earthRadius * c;
        }


        private static double DegreesToRadians(
            double degrees)
        {
            return degrees *
                   Math.PI /
                   180.0;
        }
    }


    // =============================================================
    // REQUEST
    // =============================================================

    public class DeliveryPointSearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } =
            string.Empty;
    }


    // =============================================================
    // КООРДИНАТЫ
    // =============================================================

    public class GeoCoordinates
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}