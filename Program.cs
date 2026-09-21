using ApiOzon;

var builder = WebApplication.CreateBuilder(args);

// 1. Сначала регистрируем контроллеры
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Конфигурация для подключения данных из appsettings.json
builder.Services.Configure<OzonSellerParam>(builder.Configuration.GetSection("OzonSeller"));

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

// 2. Настройка конвейера Middleware (СТРОГИЙ ПОРЯДОК)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 1. Сначала роутинг (определяем, куда идет запрос)
app.UseRouting(); 

// 2. СТРОГО ВТОРОЙ (проверяем разрешения для браузера)
app.UseCors("AllowAll"); 

// 3. Только потом передаем запрос в контроллер
app.MapControllers(); 

app.Run();