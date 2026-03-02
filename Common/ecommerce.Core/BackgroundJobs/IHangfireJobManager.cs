namespace ecommerce.Core.BackgroundJobs;

public interface IHangfireJobManager
{
    Task<string> EnqueueAsync<TJob>(object? args = null, string queue = "default", TimeSpan? delay = null);

    Task<bool> DeleteAsync(string jobId);
}