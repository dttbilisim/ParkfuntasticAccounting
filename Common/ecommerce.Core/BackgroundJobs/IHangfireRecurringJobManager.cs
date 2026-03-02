namespace ecommerce.Core.BackgroundJobs;

public interface IHangfireRecurringJobManager
{
    Task RecurAsync<TJob>(
        string cronExpression,
        object? args = null,
        string? jobId = null,
        TimeZoneInfo? timeZone = null,
        string queue = "default");

    Task DeleteAsync(string jobId);

    Task TriggerAsync(string jobId);

    Task RemoveIfExistsAsync(string jobId);
}