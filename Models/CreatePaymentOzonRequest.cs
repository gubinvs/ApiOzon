using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    // 1. Модель для запроса на создание платежа OZON
    public record CreatePaymentOzonRequest
    {
        [JsonPropertyName("accessKey")]
        public string AccessKey { get; init; } = string.Empty;

        [JsonPropertyName("amount")]
        public MoneyAmount Amount { get; init; } = new();

        [JsonPropertyName("deliverySettings")]
        public DeliverySettings? DeliverySettings { get; init; }

        [JsonPropertyName("enableFiscalization")]
        public bool EnableFiscalization { get; init; }

        [JsonPropertyName("expiresAt")]
        public DateTimeOffset? ExpiresAt { get; init; }

        [JsonPropertyName("extData")]
        public Dictionary<string, string>? ExtData { get; init; }

        [JsonPropertyName("extId")]
        public string ExtId { get; init; } = string.Empty;

        [JsonPropertyName("failUrl")]
        public string FailUrl { get; init; } = string.Empty;

        [JsonPropertyName("fiscalizationPhone")]
        public string? FiscalizationPhone { get; init; }

        [JsonPropertyName("fiscalizationType")]
        public string FiscalizationType { get; init; } = string.Empty; // Можно заменить на enum при желании

        [JsonPropertyName("items")]
        public List<OzonPaymentItem> Items { get; init; } = new();

        [JsonPropertyName("mode")]
        public string Mode { get; init; } = string.Empty;

        [JsonPropertyName("notificationUrl")]
        public string NotificationUrl { get; init; } = string.Empty;

        [JsonPropertyName("paymentAlgorithm")]
        public string PaymentAlgorithm { get; init; } = string.Empty;

        [JsonPropertyName("receiptEmail")]
        public string? ReceiptEmail { get; init; }

        [JsonPropertyName("requestSign")]
        public string RequestSign { get; init; } = string.Empty;

        [JsonPropertyName("successUrl")]
        public string SuccessUrl { get; init; } = string.Empty;
    }

    public record MoneyAmount
    {
        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; init; } = string.Empty;

        [JsonPropertyName("value")]
        public string Value { get; init; } = string.Empty; // Сумма передается строкой по спецификации
    }

    public record DeliverySettings
    {
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; init; }
    }

    public record OzonPaymentItem
    {
        [JsonPropertyName("attributes")]
        public ItemAttributes? Attributes { get; init; }

        [JsonPropertyName("extId")]
        public string ExtId { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("needMark")]
        public bool NeedMark { get; init; }

        [JsonPropertyName("positions")]
        public List<ItemPosition>? Positions { get; init; }

        [JsonPropertyName("price")]
        public MoneyAmount Price { get; init; } = new();

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; init; } // Используем decimal для поддержки дробного количества (если нужно)

        [JsonPropertyName("type")]
        public string Type { get; init; } = "TYPE_PRODUCT";

        [JsonPropertyName("sku")]
        public long Sku { get; init; }

        [JsonPropertyName("vat")]
        public string Vat { get; init; } = "VAT_0";
    }

    public record ItemAttributes
    {
        [JsonPropertyName("color")]
        public string? Color { get; init; }

        [JsonPropertyName("size")]
        public string? Size { get; init; }
    }

    public record ItemPosition
    {
        [JsonPropertyName("mark")]
        public string? Mark { get; init; }
    }
}
