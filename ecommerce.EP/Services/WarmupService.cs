using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Nest;

namespace ecommerce.EP.Services;

/// <summary>
/// Uygulama başlangıcında PostgreSQL, Redis ve Elasticsearch bağlantılarını paralel ısıtır.
/// Cold start süresini kısaltır; ilk kullanıcı isteği yavaşlamaz.
/// </summary>
public class WarmupService : IHostedService
{
    private const int DefaultWarmupTimeoutSeconds = 15;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WarmupService> _logger;

    public WarmupService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<WarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var timeoutSeconds = _configuration.GetValue("Warmup:TimeoutSeconds", DefaultWarmupTimeoutSeconds);
        _logger.LogInformation("Bağlantı havuzları paralel ısıtılıyor (timeout: {Timeout}s)...", timeoutSeconds);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var dbTask = WarmupPostgresAsync(cts.Token);
        var redisTask = WarmupRedisAsync(cts.Token);
        var elasticTask = WarmupElasticsearchAsync(cts.Token);

        await Task.WhenAll(dbTask, redisTask, elasticTask).ConfigureAwait(false);

        _logger.LogInformation("Bağlantı havuzları ısıtma tamamlandı.");
    }

    private async Task WarmupPostgresAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("PostgreSQL bağlantı havuzu ısıtıldı.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("PostgreSQL ısıtma zaman aşımına uğradı.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PostgreSQL ısıtma başarısız — uygulama çalışmaya devam ediyor.");
        }
    }

    private async Task WarmupRedisAsync(CancellationToken cancellationToken)
    {
        try
        {
            var redis = _serviceProvider.GetRequiredService<IConnectionMultiplexer>();
            var db = redis.GetDatabase();
            await db.PingAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Redis bağlantısı ısıtıldı.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Redis ısıtma zaman aşımına uğradı.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis ısıtma başarısız — uygulama çalışmaya devam ediyor.");
        }
    }

    private async Task WarmupElasticsearchAsync(CancellationToken cancellationToken)
    {
        try
        {
            var elastic = _serviceProvider.GetRequiredService<IElasticClient>();
            await elastic.PingAsync(ct: cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Elasticsearch bağlantısı ısıtıldı.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Elasticsearch ısıtma zaman aşımına uğradı.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Elasticsearch ısıtma başarısız — uygulama çalışmaya devam ediyor.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
