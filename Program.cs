using ApiOzon;
using ApiOzon.Services;
using ApiOzon.Core;
using ApiOzon.Models;
using Microsoft.EntityFrameworkCore;
using ApiOzon.Controllers;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// 1. Конфигурация для подключения данных из appsettings.json
builder.Services.Configure<OzonSellerParam>(builder.Configuration.GetSection("OzonSeller"));
builder.Services.Configure<OzonDeliveryParam>(builder.Configuration.GetSection("OzonDelivery"));
builder.Services.Configure<PasswordGuid>(builder.Configuration.GetSection("PasswordGuid"));
builder.Services.Configure<EmailSettingsParam>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<YandexGeocoderParam>(builder.Configuration.GetSection("YandexGeocoder"));

// 2. Регистрация базовых сервисов
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Заменили обычный AddHttpClient() на именованный клиент для Сбербанка с поддержкой сертификатов Минцифры
builder.Services.AddHttpClient("SberbankClient")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        handler.ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, sslErrors) =>
        {
            // Если ОС (например, Яндекс.Браузер на машине или общие настройки) доверяет сертификату — пропускаем
            if (sslErrors == SslPolicyErrors.None)
                return true;

            if (certificate == null || chain == null)
                return false;

            // Ищем папку Certificates в корне выполнения приложения
            var certsFolder = Path.Combine(AppContext.BaseDirectory, "Certificates");
            if (!Directory.Exists(certsFolder))
                certsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Certificates");

            if (Directory.Exists(certsFolder))
            {
                var certFiles = Directory.GetFiles(certsFolder, "*.*")
                    .Where(f => f.EndsWith(".cer") || f.EndsWith(".crt") || f.EndsWith(".pem"));

                foreach (var file in certFiles)
                {
                    try
                    {
                        // Безопасно загружаем локальный сертификат Минцифры (.NET 9)
                        using var localCert = X509CertificateLoader.LoadCertificateFromFile(file);
                        
                        // Прямое сопоставление: ищем, совпадает ли отпечаток нашего файла с элементами в цепочке Сбера
                        foreach (var chainElement in chain.ChainElements)
                        {
                            if (chainElement.Certificate.Thumbprint.Equals(localCert.Thumbprint, StringComparison.OrdinalIgnoreCase))
                            {
                                return true; // Нашли корневой или промежуточный сертификат Минцифры — доверяем!
                            }
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки чтения отдельных битых файлов
                    }
                }
            }

            return false;
        };

        return handler;
    });

builder.Services.AddTransient<OzonDeliveryAuthHandler>(); 
// Регистрируем триггер как Singleton
builder.Services.AddSingleton<OzonSyncTrigger>();

// Регистрируем фоновые воркеры
builder.Services.AddHostedService<OzonDeliverySyncWorker>();
builder.Services.AddScoped<OzonDeliverySyncService>(); 

// 3. Регистрация кастомных бизнес-сервисов
builder.Services.AddScoped<IOzonStockService, OzonStockService>();

// Сервис авторизации должен быть СТРОГО один (AddSingleton)
builder.Services.AddSingleton<IOzonAuthService, OzonAuthService>();
builder.Services.AddTransient<OzonAuthHandler>();

// 4. Регистрируем готовый HttpClient для работы с API Доставки Ozon
builder.Services.AddHttpClient("OzonDeliveryClient", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OzonDeliveryParam>>().Value;
    client.BaseAddress = new System.Uri($"https://{config.host}/");
})
.AddHttpMessageHandler<OzonAuthHandler>();

// 5. Подключение к базе данных интернет-магазина
var shopConnectionString = builder.Configuration["ConnectionDataShop:ConnectionDataString"];
if (string.IsNullOrEmpty(shopConnectionString))
{
    throw new Exception("Критическая ошибка: Строка подключения ConnectionDataString не найдена в конфигурации!");
}

// Подключаем MySQL
builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseMySql(shopConnectionString, ServerVersion.AutoDetect(shopConnectionString)));

// 6. 🌍 Регистрируем политику CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

// Настройка конвейера Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting(); 
app.UseCors("AllowAll"); 
app.MapControllers(); 

app.Run();
