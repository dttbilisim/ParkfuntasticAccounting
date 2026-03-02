using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ecommerce.EP.Configuration;

/// <summary>
/// API rate limiting — kullanıcıları kilitlemeden brute force ve DoS'u sınırlar.
/// Limitler appsettings "RateLimit" bölümünden okunur.
/// </summary>
public static class RateLimitingConfiguration
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimit");
        var globalPermit = section.GetValue("GlobalPermitLimit", 200);
        var windowSec = section.GetValue("WindowInSeconds", 60);
        var loginPermit = section.GetValue("LoginPermitLimit", 15);
        var authPermit = section.GetValue("AuthPermitLimit", 30);
        var publicPermit = section.GetValue("PublicPermitLimit", 120);
        var searchPermit = section.GetValue("SearchPermitLimit", 60);
        var cartPermit = section.GetValue("CartPermitLimit", 40);
        var ocrPermit = section.GetValue("OcrPermitLimit", 20);

        var window = TimeSpan.FromSeconds(windowSec);

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = globalPermit,
                        Window = window,
                        QueueLimit = 0
                    }));

            options.AddFixedWindowLimiter("login", opt =>
            {
                opt.PermitLimit = loginPermit;
                opt.Window = window;
                opt.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = authPermit;
                opt.Window = window;
                opt.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("public", opt =>
            {
                opt.PermitLimit = publicPermit;
                opt.Window = window;
                opt.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("search", opt =>
            {
                opt.PermitLimit = searchPermit;
                opt.Window = window;
                opt.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("cart", opt =>
            {
                opt.PermitLimit = cartPermit;
                opt.Window = window;
                opt.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("ocr", opt =>
            {
                opt.PermitLimit = ocrPermit;
                opt.Window = window;
                opt.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] = windowSec.ToString();
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Çok fazla istek. Lütfen kısa süre sonra tekrar deneyin." },
                    cancellationToken);
            };
        });

        return services;
    }

    private static string GetPartitionKey(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
                return $"user_{userId}";
        }
        var ip = context.Connection.RemoteIpAddress?.ToString();
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
        {
            var first = forwarded.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(first))
                ip = first;
        }
        return $"ip_{ip ?? "unknown"}";
    }
}
