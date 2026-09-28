using Microsoft.AspNetCore.Mvc;
using System.Text.Json;



[ApiController]
[Route("v1/[controller]")]
public class DeliveryPointListController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public DeliveryPointListController(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpPost]
    public async Task<IActionResult> GetDeliveryPoints(
        [FromBody] DeliveryPointListRequest request)
    {
        var client = _httpClientFactory
            .CreateClient("OzonDeliveryClient");

        // ==========================================
        // 1. Получаем список ПВЗ
        // ==========================================

        var listResponse = await client.PostAsJsonAsync(
            "v1/delivery-point/list",
            request);

        var listJson = await listResponse.Content
            .ReadAsStringAsync();

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

        if (listData == null ||
            listData.DeliveryPoints.Count == 0)
        {
            return Ok(new
            {
                delivery_points = new List<object>()
            });
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

        var infoResponse = await client.PostAsJsonAsync(
            "v1/delivery-point/info",
            infoRequest);

        var infoJson = await infoResponse.Content
            .ReadAsStringAsync();

        if (!infoResponse.IsSuccessStatusCode)
        {
            return new ContentResult
            {
                StatusCode = (int)infoResponse.StatusCode,
                ContentType = "application/json",
                Content = infoJson
            };
        }

        var infoData =
            JsonSerializer.Deserialize<DeliveryPointInfoResponse>(
                infoJson);

        if (infoData == null)
        {
            return Ok(new
            {
                delivery_points = new List<object>()
            });
        }

        // ==========================================
        // 4. Объединяем List + Info
        // ==========================================

        var result = listData.DeliveryPoints
            .Join(
                infoData.DeliveryPoints,
                listPoint => listPoint.DeliveryPointId,
                infoPoint => infoPoint.DeliveryPointId,
                (listPoint, infoPoint) => new
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
                            .FirstOrDefault()
                })
            .Where(x => x.is_active)
            .ToList();

        // ==========================================
        // 5. Возвращаем React
        // ==========================================

        return Ok(new
        {
            delivery_points = result
        });
    }
}