using ApiOzon;
using ApiOzon.Controllers;
using ApiOzon.Core;
using ApiOzon.Models;
using ApiOzon.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. КОНФИГУРАЦИЯ
// ============================================================

builder.Services.Configure<OzonSellerParam>(
    builder.Configuration.GetSection("OzonSeller"));

builder.Services.Configure<OzonDeliveryParam>(
    builder.Configuration.GetSection("OzonDelivery"));

builder.Services.Configure<PasswordGuid>(
    builder.Configuration.GetSection("PasswordGuid"));

builder.Services.Configure<EmailSettingsParam>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.Configure<YandexGeocoderParam>(
    builder.Configuration.GetSection("YandexGeocoder"));

// ============================================================
// 2. БАЗОВЫЕ СЕРВИСЫ
// ============================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// ============================================================
// 3. СЕРВИСЫ OZON
// ============================================================

builder.Services.AddTransient<OzonDeliveryAuthHandler>();
builder.Services.AddTransient<OzonAuthHandler>();

builder.Services.AddSingleton<OzonSyncTrigger>();
builder.Services.AddSingleton<IOzonAuthService, OzonAuthService>();

builder.Services.AddScoped<OzonDeliverySyncService>();
builder.Services.AddScoped<IOzonStockService, OzonStockService>();

builder.Services.AddHostedService<OzonDeliverySyncWorker>();

// ============================================================
// 4. HTTP CLIENT OZON DELIVERY
// ============================================================

builder.Services.AddHttpClient(
    "OzonDeliveryClient",
    (serviceProvider, client) =>
    {
        var config = serviceProvider
            .GetRequiredService<IOptions<OzonDeliveryParam>>()
            .Value;

        client.BaseAddress = new Uri($"https://{config.host}/");
    })
    .AddHttpMessageHandler<OzonAuthHandler>();

// ============================================================
// 5. MYSQL
// ============================================================

var shopConnectionString =
    builder.Configuration[
        "ConnectionDataShop:ConnectionDataString"];

if (string.IsNullOrWhiteSpace(shopConnectionString))
{
    throw new InvalidOperationException(
        "Не найдена строка подключения " +
        "ConnectionDataShop:ConnectionDataString.");
}

builder.Services.AddDbContext<ShopDbContext>(options =>
{
    options.UseMySql(
        shopConnectionString,
        ServerVersion.AutoDetect(shopConnectionString));
});

// ============================================================
// 6. CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true);
    });
});

// ============================================================
// 7. HTTP CLIENT ОБЫЧНОГО ИНТЕРНЕТ-ЭКВАЙРИНГА СБЕРА
// ============================================================

builder.Services.AddHttpClient<OzonPayController>();


// ============================================================
// 8. СОЗДАНИЕ ПРИЛОЖЕНИЯ
// ============================================================

var app = builder.Build();

// ============================================================
// 9. SWAGGER
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ============================================================
// 10. HTTP PIPELINE
// ============================================================

app.UseRouting();

app.UseCors("AllowAll");

app.MapControllers();

app.Run();