using System.Reflection;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Hangfire.States;
namespace ecommerce.Core.BackgroundJobs;

public class HangfireRecurringJobManager : IHangfireRecurringJobManager
{
    public Task RecurAsync<TJob>(
        string cronExpression,
        object? args = null,
        string? jobId = null,
        TimeZoneInfo? timeZone = null,
        string queue = "default",
        PerformContext? context = null)
    {
        var jobType = typeof(TJob);

        BackgroundJobArgsHelper.EnsureJobArgsType(jobType, args, out var jobArgsType);

        var queueAttr = jobType.GetCustomAttribute<QueueAttribute>(false);

        if (queueAttr != null && queue == "default")
        {
            queue = !string.IsNullOrEmpty(queueAttr.Queue) ? queueAttr.Queue : queue;
        }

        jobId ??= jobType.FullName!;

        var jobMethodCall = BackgroundJobArgsHelper.CreateJobMethodExpression<TJob>(args, jobArgsType);

        RecurringJob.AddOrUpdate(
            jobId,
            jobMethodCall,
            cronExpression,
            timeZone ?? TimeZoneInfo.Utc,
            queue
        );
        
        Console.WriteLine($"[Hangfire] Recurring job registered: {jobId} | Cron: {cronExpression} | Queue: {queue}");
        context?.WriteLine($"[Hangfire.Console] Recurring job registered: {jobId} | Cron: {cronExpression} | Queue: {queue}");
        return Task.CompletedTask;
    }
    public Task RecurAsync<TJob>(string cronExpression, object ? args = null, string ? jobId = null, TimeZoneInfo ? timeZone = null, string queue = "default"){
        var jobType = typeof(TJob);

        BackgroundJobArgsHelper.EnsureJobArgsType(jobType, args, out var jobArgsType);

        var queueAttr = jobType.GetCustomAttribute<QueueAttribute>(false);

        if (queueAttr != null && queue == "default")
        {
            queue = !string.IsNullOrEmpty(queueAttr.Queue) ? queueAttr.Queue : queue;
        }

        jobId ??= jobType.FullName!;

        var jobMethodCall = BackgroundJobArgsHelper.CreateJobMethodExpression<TJob>(args, jobArgsType);

        RecurringJob.AddOrUpdate(
            jobId,
            jobMethodCall,
            cronExpression,
            timeZone ?? TimeZoneInfo.Utc,
            queue
        );

        Console.WriteLine($"[Hangfire] Recurring job registered: {jobId} | Cron: {cronExpression} | Queue: {queue}");

        return Task.CompletedTask;
    }
    public Task DeleteAsync(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentNullException(nameof(jobId));
        }

        RecurringJob.RemoveIfExists(jobId);

        return Task.CompletedTask;
    }

    public Task TriggerAsync(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentNullException(nameof(jobId));
        }

        // FIXED: RecurringJob.TriggerJob() queue bilgisini korur
        // Hangfire'ın kendi trigger mekanizması queue bilgisini koruyor
        RecurringJob.TriggerJob(jobId);

        return Task.CompletedTask;
    }
    public Task RemoveIfExistsAsync(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentNullException(nameof(jobId));
        }

        RecurringJob.RemoveIfExists(jobId);
        return Task.CompletedTask;
    }
}