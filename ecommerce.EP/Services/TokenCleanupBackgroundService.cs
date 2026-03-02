using ecommerce.EP.Services.Abstract;

namespace ecommerce.EP.Services;

/// <summary>
/// Günlük olarak eski push token'ları temizleyen arka plan servisi.
/// Her gece 03:00'te çalışır, 30 günden eski token'ları pasif olarak işaretler.
/// EP projesinde Hangfire tam yapılandırılmadığı için BackgroundService kullanılır.
/// </summary>
public class TokenCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupBackgroundService> _logger;

    /// <summary>Çalışma periyodu — 24 saat</summary>
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    public TokenCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token temizleme arka plan servisi başlatıldı.");

        try
        {
            // İlk çalışmayı gece 03:00'e zamanla
            var initialDelay = CalculateInitialDelay();
            _logger.LogInformation(
                "İlk token temizleme {WaitDuration} sonra çalışacak (yaklaşık saat 03:00 UTC).",
                initialDelay);

            await Task.Delay(initialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunCleanupTask(stoppingToken);
                await Task.Delay(RunInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host kapatılırken Task.Delay veya RunCleanupTask iptal edildi — normal durum
        }
        catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Aynı şekilde Task.Delay iptalinde TaskCanceledException da fırlayabilir
        }

        _logger.LogInformation("Token temizleme arka plan servisi durduruluyor.");
    }

    /// <summary>
    /// Token temizleme görevini çalıştırır. Hata durumunda servis çalışmaya devam eder.
    /// </summary>
    private async Task RunCleanupTask(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Günlük token temizleme görevi başlatılıyor...");

            // Scoped servis oluştur (DbContext scoped olduğu için)
            using var scope = _serviceProvider.CreateScope();
            var tokenCleanupService = scope.ServiceProvider.GetRequiredService<ITokenCleanupService>();

            var cleanedCount = await tokenCleanupService.CleanupExpiredTokensAsync();

            _logger.LogInformation(
                "Günlük token temizleme görevi tamamlandı. Temizlenen: {CleanedCount}",
                cleanedCount);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapatılıyor, normal durum
            _logger.LogInformation("Token temizleme görevi iptal edildi (uygulama kapatılıyor).");
        }
        catch (Exception ex)
        {
            // Hata olsa bile servis çalışmaya devam etmeli
            _logger.LogError(ex, "Günlük token temizleme görevi sırasında hata oluştu. Bir sonraki çalışmada tekrar denenecek.");
        }
    }

    /// <summary>
    /// İlk çalışma için gece 03:00 UTC'ye kadar beklenecek süreyi hesaplar.
    /// Eğer saat 03:00 geçmişse, ertesi gün 03:00'e zamanlar.
    /// </summary>
    private static TimeSpan CalculateInitialDelay()
    {
        var now = DateTime.UtcNow;
        var todayAt03 = now.Date.AddHours(3);

        // Saat 03:00 geçmişse ertesi gün 03:00'e zamanla
        if (now >= todayAt03)
        {
            todayAt03 = todayAt03.AddDays(1);
        }

        return todayAt03 - now;
    }
}
