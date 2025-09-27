// 📍 /src/TelegramBlock/Filters/AdminTokenAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TelegramBlock.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class AdminTokenAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            var envToken = Environment.GetEnvironmentVariable("ADMIN_TOKEN");
            if (string.IsNullOrWhiteSpace(envToken))
            {
                ctx.Result = new ObjectResult(new { error = "admin_token_unset" }) { StatusCode = 503 };
                return;
            }

            if (!ctx.HttpContext.Request.Headers.TryGetValue("X-Admin-Token", out var provided) ||
                !string.Equals(provided.ToString(), envToken, StringComparison.Ordinal))
            {
                ctx.Result = new UnauthorizedObjectResult(new { error = "admin_token_invalid" });
                return;
            }

            await next();
        }
    }
}
