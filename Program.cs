using ApiOzon;
using ApiOzon.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Конфигурация для подключения данных из appsettings.json
builder.Services.Configure<OzonSellerParam>(builder.Configuration.GetSection("OzonSeller"));
builder.Services.Configure<OzonDeliveryParam>(builder.Configuration.GetSection("OzonDelivery"));
builder.Services.Configure<PasswordGuid>(builder.Configuration.GetSection("PasswordGuid"));
builder.Services.Configure<EmailSettingsParam>(builder.Configuration.GetSection("EmailSettings"));

// 2. Регистрация базовых сервисов
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(); // Базовый фабричный клиент

// 3. Регистрация кастомных бизнес-сервисов (БЕЗ ДУБЛИКАТОВ)
builder.Services.AddScoped<IOzonStockService, OzonStockService>();

// Сервис авторизации должен быть СТРОГО один (AddSingleton), чтобы держать кэш токена и testcookie
builder.Services.AddSingleton<IOzonAuthService, OzonAuthService>();
builder.Services.AddTransient<OzonAuthHandler>();

// 4. Регистрируем готовый HttpClient для работы с API Доставки Ozon
builder.Services.AddHttpClient("OzonDeliveryClient", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OzonDeliveryParam>>().Value;
    
    // Строго базовый URL без лишних путей (превращается в https://api-delivery.ozon.ru/)
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
              .SetIsOriginAllowed(_ => true); // Разрешает запросы с любых сайтов/портов
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
