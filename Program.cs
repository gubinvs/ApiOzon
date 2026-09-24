using ApiOzon;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Сначала регистрируем контроллеры
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Конфигурация для подключения данных из appsettings.json
builder.Services.Configure<OzonSellerParam>(builder.Configuration.GetSection("OzonSeller"));
builder.Services.Configure<OzonDeliveryParam>(builder.Configuration.GetSection("OzonDelivery"));
builder.Services.Configure<PasswordGuid>(builder.Configuration.GetSection("PasswordGuid"));
builder.Services.Configure<EmailSettingsParam>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IOzonStockService, OzonStockService>();

// Извлекаем готовую строку из appsettings.json для подключения к базе данных интернет магазина
var shopConnectionString = builder.Configuration["ConnectionDataShop:ConnectionDataString"];
if (string.IsNullOrEmpty(shopConnectionString))
{
    throw new Exception("Критическая ошибка: Строка подключения ConnectionDataString не найдена в конфигурации!");
}

// Подключаем MySQL
builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseMySql(shopConnectionString, ServerVersion.AutoDetect(shopConnectionString)));

// 🌍 Регистрируем политику CORS
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

// Сначала роутинг
app.UseRouting(); 

// Политика безопасности
app.UseCors("AllowAll"); 

// Только потом передаем запрос в контроллер
app.MapControllers(); 

app.Run();