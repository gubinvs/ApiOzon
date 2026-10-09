using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
        // Модель для запроса на создание платежа внутри вашей системы
        public record CreatePaymentOzonRequest (
            string OrderId, 
            decimal Amount, 
            string Description, 
            string CustomerEmail
        );

        // Модель запроса к API Ozon Pay
        public record OzonCreatePaymentRequest(
            [property: JsonPropertyName("order_number")] string OrderNumber,
            [property: JsonPropertyName("amount")] int AmountInKopecks, // Ozon обычно принимает в копейках
            [property: JsonPropertyName("currency")] string Currency = "RUB",
            [property: JsonPropertyName("description")] string? Description = null
            // Сюда также можно добавить объект "receipt" для 54-ФЗ
        );

        // Модель ответа от API Ozon Pay
        public record OzonPaymentResponse(
            [property: JsonPropertyName("payment_id")] string PaymentId,
            [property: JsonPropertyName("payment_url")] string PaymentUrl,
            [property: JsonPropertyName("status")] string Status
        );

        // Модель Webhook от Ozon Pay
        public record OzonWebhookPayload(
            [property: JsonPropertyName("payment_id")] string PaymentId,
            [property: JsonPropertyName("order_number")] string OrderNumber,
            [property: JsonPropertyName("status")] string Status,
            [property: JsonPropertyName("amount")] int Amount
        );

}