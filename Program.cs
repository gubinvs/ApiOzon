
using ApiOzon;
using ApiOzon.Services;
using ApiOzon.Core;
using ApiOzon.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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

builder.Services.AddHttpClient();

// ============================================================
// 3. СЕРВИСЫ OZON DELIVERY
// ============================================================

builder.Services.AddTransient<OzonDeliveryAuthHandler>();

builder.Services.AddSingleton<OzonSyncTrigger>();

builder.Services.AddHostedService<OzonDeliverySyncWorker>();

builder.Services.AddScoped<OzonDeliverySyncService>();

builder.Services.AddScoped<IOzonStockService, OzonStockService>();

builder.Services.AddSingleton<IOzonAuthService, OzonAuthService>();

builder.Services.AddTransient<OzonAuthHandler>();

builder.Services.AddMemoryCache();

// ============================================================
// 4. HTTP CLIENT OZON DELIVERY
// ============================================================

builder.Services.AddHttpClient(
    "OzonDeliveryClient",
    (serviceProvider, client) =>
    {
        var config = serviceProvider
            .GetRequiredService<
                Microsoft.Extensions.Options.IOptions<OzonDeliveryParam>>()
            .Value;

        client.BaseAddress = new Uri(
            $"https://{config.host}/");
    })
    .AddHttpMessageHandler<OzonAuthHandler>();

// ============================================================
// 5. ПОДКЛЮЧЕНИЕ К БАЗЕ ДАННЫХ SHOP
// ============================================================

var shopConnectionString =
    builder.Configuration[
        "ConnectionDataShop:ConnectionDataString"];

if (string.IsNullOrEmpty(shopConnectionString))
{
    throw new Exception(
        "Критическая ошибка: Строка подключения " +
        "ConnectionDataString не найдена в конфигурации!");
}

builder.Services.AddDbContext<ShopDbContext>(
    options =>
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
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .SetIsOriginAllowed(_ => true);
        });
});

// ============================================================
// 7. СЕРТИФИКАТ СБЕРА
// ============================================================

// ------------------------------------------------------------
// Клиентский сертификат Sber Business API
// ------------------------------------------------------------

var certificatePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "SBBAPI_92418_bdc59fc8-db9a-4dd6-b1e2-6da4a202e772.p12");

if (!File.Exists(certificatePath))
{
    throw new FileNotFoundException(
        $"Сертификат Сбера не найден: {certificatePath}");
}

// ------------------------------------------------------------
// Настройки Sberbank из appsettings.json
// ------------------------------------------------------------

var sberBaseUrl =
    builder.Configuration["Sberbank:BaseUrl"];

var certificatePassword =
    builder.Configuration["Sberbank:CertificatePassword"];

if (string.IsNullOrWhiteSpace(sberBaseUrl))
{
    throw new Exception(
        "Критическая ошибка: Sberbank:BaseUrl " +
        "не найден в appsettings.json!");
}

if (string.IsNullOrWhiteSpace(certificatePassword))
{
    throw new Exception(
        "Критическая ошибка: Sberbank:CertificatePassword " +
        "не найден в appsettings.json!");
}

// ------------------------------------------------------------
// Загружаем PKCS#12 сертификат
// ------------------------------------------------------------

var sberCertificate =
    X509CertificateLoader.LoadPkcs12FromFile(
        certificatePath,
        certificatePassword,
        X509KeyStorageFlags.MachineKeySet);

// ------------------------------------------------------------
// Проверяем наличие закрытого ключа
// ------------------------------------------------------------

if (!sberCertificate.HasPrivateKey)
{
    throw new Exception(
        "Критическая ошибка: сертификат Сбера " +
        "не содержит закрытого ключа!");
}

// ============================================================
// 8. СЕРТИФИКАТЫ ЦЕПОЧКИ СБЕРА
// ============================================================

// ------------------------------------------------------------
// Корневой сертификат SberCA Root Ext
//
// Файл:
// prom-certs/sberca-root-ext.crt
// ------------------------------------------------------------

var sberRootCaPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "prom-certs",
    "sberca-root-ext.crt");

if (!File.Exists(sberRootCaPath))
{
    throw new FileNotFoundException(
        $"Корневой сертификат Сбера не найден: {sberRootCaPath}");
}

var sberRootCaCertificate =
    X509CertificateLoader.LoadCertificateFromFile(
        sberRootCaPath);

// ------------------------------------------------------------
// Промежуточный сертификат SberCA Ext
//
// Файл:
// prom-certs/sberca-ext.crt
// ------------------------------------------------------------

var sberIntermediatePath = Path.Combine(
    builder.Environment.ContentRootPath,
    "prom-certs",
    "sberca-ext.crt");

if (!File.Exists(sberIntermediatePath))
{
    throw new FileNotFoundException(
        $"Промежуточный сертификат Сбера не найден: {sberIntermediatePath}");
}

var sberIntermediateCertificate =
    X509CertificateLoader.LoadCertificateFromFile(
        sberIntermediatePath);

// ============================================================
// 9. HTTP CLIENT SBER BUSINESS API
// ============================================================

builder.Services.AddHttpClient(
    "SberBusinessClient",
    client =>
    {
        client.BaseAddress = new Uri(
            sberBaseUrl.TrimEnd('/') + "/");

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        // ----------------------------------------------------
        // Клиентский сертификат
        // ----------------------------------------------------

        handler.ClientCertificates.Add(
            sberCertificate);

        // ----------------------------------------------------
        // Проверка сертификата сервера
        // ----------------------------------------------------

  handler.ServerCertificateCustomValidationCallback =
    (request, certificate, chain, sslPolicyErrors) =>
    {
        if (certificate == null)
        {
            return false;
        }

        // ----------------------------------------------------
        // Сертификат сервера
        // ----------------------------------------------------

        var serverCertificate =
            new X509Certificate2(certificate);

        // ----------------------------------------------------
        // Строим собственную доверенную цепочку Sberbank
        //
        // fintech-test.sberbank.ru
        //          ↓
        //      SberCA Ext
        //          ↓
        //    SberCA Root Ext
        // ----------------------------------------------------

        using var customChain = new X509Chain();

        customChain.ChainPolicy.TrustMode =
            X509ChainTrustMode.CustomRootTrust;

        customChain.ChainPolicy.CustomTrustStore.Add(
            sberRootCaCertificate);

        customChain.ChainPolicy.ExtraStore.Add(
            sberIntermediateCertificate);

        customChain.ChainPolicy.RevocationMode =
            X509RevocationMode.NoCheck;

        customChain.ChainPolicy.VerificationFlags =
            X509VerificationFlags.NoFlag;

        var chainValid =
            customChain.Build(serverCertificate);

        if (!chainValid)
        {
            Console.WriteLine(
                "Sberbank server certificate chain is invalid.");

            foreach (var status in customChain.ChainStatus)
            {
                Console.WriteLine(
                    $"{status.Status}: {status.StatusInformation}");
            }

            return false;
        }

        // ----------------------------------------------------
        // Проверяем имя сервера.
        //
        // Не используем RemoteCertificateNameMismatch,
        // поскольку системная проверка macOS/.NET не знает
        // наш приватный SberCA и в данном случае возвращает
        // ложную ошибку.
        //
        // Получаем DNS-имя сертификата и сравниваем его
        // с фактическим host запроса.
        // ----------------------------------------------------

        var certificateDnsName =
            serverCertificate.GetNameInfo(
                X509NameType.DnsName,
                false);

        var requestHost =
            request.RequestUri?.Host;

        var nameValid =
            !string.IsNullOrWhiteSpace(certificateDnsName) &&
            !string.IsNullOrWhiteSpace(requestHost) &&
            string.Equals(
                certificateDnsName,
                requestHost,
                StringComparison.OrdinalIgnoreCase);

        Console.WriteLine(
            $"Sber certificate DNS name: {certificateDnsName}");

        Console.WriteLine(
            $"Sber request host: {requestHost}");

        Console.WriteLine(
            $"Sber certificate chain valid: {chainValid}");

        Console.WriteLine(
            $"Sber certificate name valid: {nameValid}");

        return chainValid && nameValid;
    };
        return handler;
    });

// ============================================================
// 10. СОЗДАНИЕ APPLICATION
// ============================================================

var app = builder.Build();

// ============================================================
// 11. SWAGGER
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

// ============================================================
// 12. HTTP PIPELINE
// ============================================================

app.UseRouting();

app.UseCors("AllowAll");

app.MapControllers();

app.Run();