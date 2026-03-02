using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ecommerce.Core.BackgroundJobs;

/// <summary>
/// Development ortamında job'ları senkron olarak çalıştıran Hangfire yöneticisi.
/// Hangfire altyapısı olmadan job'ların test edilmesini sağlar.
/// </summary>
public class NullHangfireJobManager : IHangfireJobManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NullHangfireJobManager> _logger;

    public NullHangfireJobManager(IServiceProvider serviceProvider, ILogger<NullHangfireJobManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<string> EnqueueAsync<TJob>(object? args = null, string queue = "default", TimeSpan? delay = null)
    {
        var jobId = Guid.NewGuid().ToString();
        _logger.LogInformation(
            "[Dev] Job senkron çalıştırılıyor: {JobType}, Args: {ArgsType}",
            typeof(TJob).Name, args?.GetType().Name ?? "null");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var job = scope.ServiceProvider.GetRequiredService<TJob>();

            // IAsyncBackgroundJob<TArgs> interface'ini bul ve çalıştır
            var jobType = typeof(TJob);
            var interfaces = jobType.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IAsyncBackgroundJob<>))
                {
                    var method = iface.GetMethod("ExecuteAsync");
                    if (method != null && args != null)
                    {
                        var task = (Task)method.Invoke(job, new[] { args })!;
                        await task;
                        _logger.LogInformation("[Dev] Job başarıyla tamamlandı: {JobType}", typeof(TJob).Name);
                        return jobId;
                    }
                }
            }

            // Parametresiz IAsyncBackgroundJob
            if (job is IAsyncBackgroundJob asyncJob)
            {
                await asyncJob.ExecuteAsync();
                _logger.LogInformation("[Dev] Job başarıyla tamamlandı: {JobType}", typeof(TJob).Name);
                return jobId;
            }

            _logger.LogWarning("[Dev] Job çalıştırılamadı — uygun interface bulunamadı: {JobType}", typeof(TJob).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Dev] Job çalıştırılırken hata oluştu: {JobType}", typeof(TJob).Name);
        }

        return jobId;
    }

    public Task<bool> DeleteAsync(string jobId)
    {
        return Task.FromResult(true);
    }
}

public class NullHangfireRecurringJobManager : IHangfireRecurringJobManager
{
    public Task RecurAsync<TJob>(string cronExpression, object? args = null, string? jobId = null, TimeZoneInfo? timeZone = null, string queue = "default")
    {
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string jobId)
    {
        return Task.CompletedTask;
    }

    public Task TriggerAsync(string jobId)
    {
        return Task.CompletedTask;
    }

    public Task RemoveIfExistsAsync(string jobId)
    {
        return Task.CompletedTask;
    }
}
