using System;
using System.Text.Json;
using System.Threading.Tasks;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ecommerce.Web.Domain.MiddleWares;

public class OnlineUserTrackingMiddleware
{
    private readonly RequestDelegate _next;

    public OnlineUserTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Value?.StartsWith("/assets") == true ||
            context.Request.Path.Value?.StartsWith("/_blazor") == true || 
            context.Request.Path.Value?.StartsWith("/_framework") == true ||
            context.Request.Path.Value?.StartsWith("/_content") == true ||
            context.Request.Path.Value?.StartsWith("/health") == true ||
            context.Request.Path.Value?.EndsWith(".js") == true ||
            context.Request.Path.Value?.EndsWith(".css") == true ||
            context.Request.Path.Value?.EndsWith(".ico") == true ||
            context.Request.Path.Value?.EndsWith(".png") == true ||
            context.Request.Path.Value?.EndsWith(".jpg") == true ||
            context.Request.Path.Value?.EndsWith(".jpeg") == true ||
            context.Request.Path.Value?.EndsWith(".svg") == true ||
            context.Request.Path.Value?.EndsWith(".gif") == true ||
            context.Request.Path.Value?.EndsWith(".woff") == true ||
            context.Request.Path.Value?.EndsWith(".woff2") == true ||
            context.Request.Path.Value?.EndsWith(".ttf") == true ||
            context.Request.Path.Value?.EndsWith(".map") == true)
        {
            await _next(context);
            return;
        }

        // Only track authenticated users (or you can track everyone if needed)
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            try
            {
                var redisService = context.RequestServices.GetService<IRedisCacheService>();
                if (redisService != null)
                {
                    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var username = context.User.Identity.Name;
                    
                    // Construct DTO
                    var dto = new OnlineUserDto
                    {
                        UserId = userId ?? "Anonymous",
                        Username = username ?? "Anonymous",
                        FullName = username, // Can be improved if claims have full name
                        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        LastPageUrl = context.Request.Path + context.Request.QueryString,
                        LastActiveTime = DateTime.UtcNow,
                        Application = "Web"
                    };

                    var json = JsonSerializer.Serialize(dto);
                    
                    // Use a Sorted Set with Score = Timestamp (ticks)
                    // Key: "online_users"
                    // Member: JSON (Unique? No, user can change page. Use UserId as unique identifier in member? 
                    // Issue: If member is JSON, changing LastPageUrl changes member status, so duplicate entries per user.
                    // Solution: Use Key="online_users", Member="UserId". Store details in a separate hash "online_user_details:{UserId}"?
                    // OR: Use Member = UserId. Store details in a separate key.
                    // Better for list: Key="online_users", Member=UserId. Score=Timestamp.
                    // And store detail in Key="online_user:{UserId}" (String or Hash).
                    
                    // Use Unix Timestamp (Seconds) for Score to avoid precision loss in Redis double
                    var score = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
                    
                    // 1. Update Score in Sorted Set
                    await redisService.AddToSortedSetAsync("online_users", userId, score);
                    
                    // 2. Update Detail Key (Expire in 10 mins)
                    await redisService.SetAsync($"online_user_detail:{userId}", dto, TimeSpan.FromMinutes(10));
                }
            }
            catch (Exception)
            {
                // Fire and forget, don't block request
            }
        }

        await _next(context);
    }
}
