using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using TelegramBlock.Controllers;
using TelegramBlock.DbContext;
using TelegramBlock.Entities;
using TelegramBlock.Logic;
using TelegramBlock.Startup;

var builder = WebApplication.CreateBuilder(args);

// Контроллеры, Swagger, зависимости
builder.Services.AddControllers();
// Позволяем enum-ам сериализоваться строками ("Left"/"Right")
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(o =>
{
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    o.SerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger(); // до Build()

/*builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Digitaline TS API", Version = "v1" });

    // 🔒 Добавляем описание схемы авторизации
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "Введите JWT токен в формате: Bearer {your token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
}); */


// загрузка .env
Env.Load();

var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "db";
var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var db = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "telegramblock";
var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
var pwd = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "super_secure_password";

var connStr = $"Host={host};Port={port};Database={db};Username={user};Password={pwd}";

builder.Services.AddScoped<IMoveService, MoveService>();
builder.Services.AddScoped<IBurnAndDropService, BurnAndDropService>();
// Фактический спаунер реализован в FiguresController-холсте как FigureSpawnService — зарегистрируем как ISpawnService
builder.Services.AddScoped<ISpawnService, FigureSpawnService>();
builder.Services.AddScoped<ITurnOrchestrator, TurnOrchestrator>();
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseNpgsql(connStr));

var app = builder.Build();

// Автоматическое применение миграций при запуске с аргументом "migrate"
if (args.Contains("migrate"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();

    Thread.Sleep(5000); // дать PostgreSQL время на старт перед первой попыткой

    var retries = 15;
    while (retries > 0)
    {
        try
        {
            context.Database.Migrate();
            Console.WriteLine("✅ Миграции применены.");
            
            // После успешных миграций — сид словаря
            await LexemeSeeder.SeedAsync(context);
         break;
        }
        catch (Exception ex)
        {
            retries--;
            Console.WriteLine($"⏳ Ожидание PostgreSQL ({15 - retries}/15)... {ex.Message}");
            Thread.Sleep(3000);
        }
    }
}

// Swagger
app.UseSwaggerWithUI();

app.UseRouting();
// app.UseAuthentication();
// app.UseAuthorization();
app.MapControllers();

app.Run();