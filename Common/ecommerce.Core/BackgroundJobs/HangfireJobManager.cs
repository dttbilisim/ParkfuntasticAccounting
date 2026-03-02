using System.Reflection;
using Hangfire;
using Hangfire.States;

namespace ecommerce.Core.BackgroundJobs;

public class HangfireJobManager : IHangfireJobManager
{
    private static readonly Lazy<IBackgroundJobClient> CachedClient =
        new(
            () => new BackgroundJobClient(),
            LazyThreadSafetyMode.PublicationOnly
        );

    private static readonly Func<IBackgroundJobClient> DefaultFactory = () => CachedClient.Value;
    private static readonly object ClientFactoryLock = new object();
    private static Func<IBackgroundJobClient>? _clientFactory;

    public static Func<IBackgroundJobClient> ClientFactory
    {
        get
        {
            lock (ClientFactoryLock)
                return _clientFactory ?? DefaultFactory;
        }
        set
        {
            lock (ClientFactoryLock)
                _clientFactory = value;
        }
    }

    public Task<string> EnqueueAsync<TJob>(object? args = null, string queue = "default", TimeSpan? delay = null)
    {
        var jobType = typeof(TJob);

        BackgroundJobArgsHelper.EnsureJobArgsType(jobType, args, out var jobArgsType);

        var queueAttr = jobType.GetCustomAttribute<QueueAttribute>(false);

        if (queueAttr != null && queue == "default")
        {
            queue = !string.IsNullOrEmpty(queueAttr.Queue) ? queueAttr.Queue : queue;
        }

        var jobMethodCall = BackgroundJobArgsHelper.CreateJobMethodExpression<TJob>(args, jobArgsType);

        var jobUniqueIdentifier = !delay.HasValue
            ? ClientFactory().Create(jobMethodCall, new EnqueuedState(queue))
            : ClientFactory().Create(jobMethodCall, new ScheduledState(delay.Value));

        return Task.FromResult(jobUniqueIdentifier);
    }

    public Task<bool> DeleteAsync(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentNullException(nameof(jobId));
        }

        var successfulDeletion = ClientFactory().Delete(jobId);

        return Task.FromResult(successfulDeletion);
    }
}