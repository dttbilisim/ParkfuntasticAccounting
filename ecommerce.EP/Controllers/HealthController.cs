using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ecommerce.EFCore.Context;
using StackExchange.Redis;
using Nest;

namespace ecommerce.EP.Controllers;

/// <summary>
/// Sağlık kontrolü — bağlantı havuzlarını ısıtmak için hafif endpoint.
/// Auth gerektirmez, mobil uygulama foreground'a geldiğinde çağırır.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("public")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly IElasticClient _elastic;

    public HealthController(
        ApplicationDbContext context,
        IConnectionMultiplexer redis,
        IElasticClient elastic)
    {
        _context = context;
        _redis = redis;
        _elastic = elastic;
    }

    /// <summary>
    /// Hafif ping — PostgreSQL, Redis ve Elasticsearch bağlantılarını ısıtır.
    /// Mobil uygulama arka plandan döndüğünde bu endpoint'i çağırarak
    /// ilk gerçek isteğin yavaş olmasını önler.
    /// </summary>
    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        try
        {
            // PostgreSQL bağlantı havuzunu ısıt
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");

            // Redis bağlantısını kontrol et
            var db = _redis.GetDatabase();
            await db.PingAsync();

            // Elasticsearch bağlantısını kontrol et
            await _elastic.PingAsync();

            return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
        }
        catch (Exception)
        {
            // Warm-up başarısız olsa bile 200 dön — amaç bağlantıları ısıtmak
            return Ok(new { status = "degraded", timestamp = DateTime.UtcNow });
        }
    }
}
