// 📂 Проект: Telegram MiniApp "Блок-Башня"
// 📄 Назначение: Подключение Swagger UI
// 📍 Путь: /src/TelegramBlock/Startup/SwaggerSetup.cs

using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace TelegramBlock.Startup
{
    public static class SwaggerSetup
    {
        public static void AddSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "TelegramBlock API",
                    Version = "v1",
                    Description = "Миниапп на базе Telegram и .NET"
                });
            });
        }

        public static void UseSwaggerWithUI(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TelegramBlock API v1");
                c.RoutePrefix = string.Empty;
            });
        }
    }
}
