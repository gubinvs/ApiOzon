using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    // 1. Модель для запроса на создание платежа внутри вашей системы (от фронта к вашему API)
    public record CreatePaymentOzonRequest
    {
        [Required(ErrorMessage = "Укажите идентификатор заказа")]
        public string OrderId { get; init; } = null!;

        [Required(ErrorMessage = "Укажите сумму")]
        [Range(0.01, 1000000, ErrorMessage = "Сумма должна быть от 0.01 до 1 000 000 рублей")]
        public decimal Amount { get; init; }

        public string? Description { get; init; }

        [Required(ErrorMessage = "Укажите email покупателя")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string CustomerEmail { get; init; } = null!;
    }

    // 2. Статусы платежа (строгая типизация)
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OzonPaymentStatus
    {
        CREATED,
        PENDING,
        SUCCEEDED,
        FAILED,
        CANCELLED,
        EXPIRED
    }

    // 3. Модель запроса к API Ozon Pay (включая чеки 54-ФЗ)
    public record OzonCreatePaymentRequest(
        // 1. Обязательные параметры (сначала)
        [property: JsonPropertyName("order_number")] string OrderNumber,
        [property: JsonPropertyName("amount")] int AmountInKopecks,
        [property: JsonPropertyName("customer_email")] string CustomerEmail,
        [property: JsonPropertyName("return_url")] string ReturnUrl,
        [property: JsonPropertyName("receipt")] OzonReceipt Receipt,
        
        // 2. Необязательные параметры (в конце, со значениями по умолчанию)
        [property: JsonPropertyName("currency")] string Currency = "RUB",
        [property: JsonPropertyName("description")] string? Description = null
    );

    // Вспомогательные модели для чека
    public record OzonReceipt(
        [property: JsonPropertyName("customer")] string CustomerEmail,
        [property: JsonPropertyName("items")] List<OzonReceiptItem> Items,
        [property: JsonPropertyName("tax_system_code")] int TaxSystemCode = 1 // Общая система налогообложения (ОСНО)
    );

    public record OzonReceiptItem(
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("quantity")] int Quantity,
        [property: JsonPropertyName("amount")] int AmountInKopecks,
        [property: JsonPropertyName("vat_code")] int VatCode = 2 // НДС 20%
    );

    // 4. Модель ответа от API Ozon Pay
    public record OzonPaymentResponse(
        [property: JsonPropertyName("payment_id")] string PaymentId,
        [property: JsonPropertyName("payment_url")] string PaymentUrl,
        // Добавляем конвертер прямо к свойству
        [property: JsonPropertyName("status")]
        [property: JsonConverter(typeof(JsonStringEnumConverter))] 
        OzonPaymentStatus Status
    );

    // 5. Модель Webhook от Ozon Pay
    public record OzonWebhookPayload(
        [property: JsonPropertyName("payment_id")] string PaymentId,
        [property: JsonPropertyName("order_number")] string OrderNumber,
        // Добавляем конвертер прямо к свойству
        [property: JsonPropertyName("status")]
        [property: JsonConverter(typeof(JsonStringEnumConverter))] 
        OzonPaymentStatus Status,
        [property: JsonPropertyName("amount")] int AmountInKopecks
    );
}