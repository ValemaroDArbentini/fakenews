// 📂 Проект: Telegram MiniApp "Блок-Башня"
// 📄 Назначение: Health-проверка и базовая информация
// 📍 Путь: /src/TelegramBlock/Controllers/HealthController.cs

using Microsoft.AspNetCore.Mvc;

namespace TelegramBlock.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new
        {
            status = "ok",
            service = "TelegramBlock",
            version = "1.0.0",
            time = DateTime.UtcNow
        });
    }
}
