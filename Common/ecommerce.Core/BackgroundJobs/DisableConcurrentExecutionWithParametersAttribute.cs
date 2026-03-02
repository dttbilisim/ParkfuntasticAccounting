using System.IO.Hashing;
using System.Text;
using Hangfire.Common;
using Hangfire.Server;

#pragma warning disable CS0618

namespace ecommerce.Core.BackgroundJobs;

public class DisableConcurrentExecutionWithParametersAttribute : JobFilterAttribute, IServerFilter
{
    private readonly int _timeoutInSeconds;

    public DisableConcurrentExecutionWithParametersAttribute(int timeoutInSeconds)
    {
        if (timeoutInSeconds < 0) throw new ArgumentException("Timeout argument value should be greater that zero.");

        _timeoutInSeconds = timeoutInSeconds;
    }

    public void OnPerforming(PerformingContext filterContext)
    {
        if (filterContext.BackgroundJob.Job == null)
        {
            return;
        }

        var resource = GetResource(filterContext.BackgroundJob.Job);

        var timeout = TimeSpan.FromSeconds(_timeoutInSeconds);

        try
        {
            var distributedLock = filterContext.Connection.AcquireDistributedLock(resource, timeout);

            filterContext.Items["DistributedLock"] = distributedLock;
        }
        catch
        {
            filterContext.Canceled = true;
        }
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        if (!filterContext.Items.ContainsKey("DistributedLock"))
        {
            throw new InvalidOperationException("Can not release a distributed lock: it was not acquired.");
        }

        var distributedLock = (IDisposable) filterContext.Items["DistributedLock"];
        distributedLock?.Dispose();
    }

    private static string GetFingerprint(Job job)
    {
        var parameters = string.Empty;
        if (job.Arguments != null)
        {
            parameters = string.Join(".", job.Arguments);
        }

        if (job.Type == null || job.Method == null)
        {
            return string.Empty;
        }

        var payload = $"{job.Type.FullName}.{job.Method.Name}.{parameters}";

        var fingerprint = string.Concat(XxHash64.Hash(Encoding.UTF8.GetBytes(payload)).Select(x => x.ToString("X2")));

        return fingerprint;
    }

    private static string GetResource(Job job)
    {
        return GetFingerprint(job);
    }
}