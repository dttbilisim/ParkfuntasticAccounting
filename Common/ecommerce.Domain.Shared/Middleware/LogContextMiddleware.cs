using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace ecommerce.Domain.Shared.Middleware;

public class LogContextMiddleware
{
    private readonly RequestDelegate _next;

    public LogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var username = context.User?.Identity?.IsAuthenticated == true ? context.User.Identity.Name : "Anonymous";
        var userId = context.User?.FindFirst("sub")?.Value ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // X-Forwarded-For check for proxies
        if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        }

        using (LogContext.PushProperty("ClientIp", ipAddress))
        using (LogContext.PushProperty("Username", username))
        using (LogContext.PushProperty("UserId", userId))
        {
            await _next(context);
        }
    }
}
