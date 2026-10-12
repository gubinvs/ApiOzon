using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ApiOzon.Models
{
    // 2. Модель для ответа на создание платежа OZON
    public record CreatePaymentOzonResponse
    {
        [JsonPropertyName("extData")]
        public Dictionary<string, string>? ExtData { get; init; }

        [JsonPropertyName("order")]
        public OzonOrder Order { get; init; } = new();
    }

    public record OzonOrder
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("number")]
        public string Number { get; init; } = string.Empty;

        [JsonPropertyName("extId")]
        public string ExtId { get; init; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty; // Можно Enum (STATUS_NEW и т.д.)

        [JsonPropertyName("mode")]
        public string Mode { get; init; } = string.Empty;

        [JsonPropertyName("paymentAlgorithm")]
        public string PaymentAlgorithm { get; init; } = string.Empty;

        [JsonPropertyName("fiscalizationType")]
        public string FiscalizationType { get; init; } = string.Empty;

        [JsonPropertyName("payLink")]
        public string? PayLink { get; init; }

        [JsonPropertyName("isTestMode")]
        public bool IsTestMode { get; init; }

        [JsonPropertyName("expiresAt")]
        public DateTimeOffset? ExpiresAt { get; init; }

        [JsonPropertyName("deliverySettings")]
        public OrderDeliverySettings? DeliverySettings { get; init; }

        [JsonPropertyName("remainingAmount")]
        public MoneyAmount RemainingAmount { get; init; } = new();

        [JsonPropertyName("items")]
        public List<OzonResponseItem> Items { get; init; } = new();
    }

    public record OrderDeliverySettings
    {
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; init; }

        [JsonPropertyName("fixedPrice")]
        public MoneyAmount? FixedPrice { get; init; }
    }

    public record OzonResponseItem
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("extId")]
        public string ExtId { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("sku")]
        public long Sku { get; init; }

        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        [JsonPropertyName("vat")]
        public string Vat { get; init; } = string.Empty;

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; init; }

        [JsonPropertyName("needMark")]
        public bool NeedMark { get; init; }

        [JsonPropertyName("price")]
        public MoneyAmount Price { get; init; } = new();

        [JsonPropertyName("attributes")]
        public Dictionary<string, string>? Attributes { get; init; } // Изменено на Dictionary, так как в JSON пришли абстрактные property1, property2

        [JsonPropertyName("positions")]
        public List<OzonResponsePosition>? Positions { get; init; }
    }

    public record OzonResponsePosition
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        [JsonPropertyName("mark")]
        public string? Mark { get; init; }

        [JsonPropertyName("markIsMissing")]
        public bool MarkIsMissing { get; init; }

        [JsonPropertyName("markIsReadonly")]
        public bool MarkIsReadonly { get; init; }

        [JsonPropertyName("refundType")]
        public string? RefundType { get; init; }

        [JsonPropertyName("price")]
        public MoneyAmount Price { get; init; } = new();

        [JsonPropertyName("remainingAmount")]
        public MoneyAmount RemainingAmount { get; init; } = new();
    }
}
