using System.Globalization;
using System.IO.Hashing;
using System.Text;
using Hangfire.Client;
using Hangfire.Common;
using Hangfire.Server;
using Hangfire.Storage;

namespace ecommerce.Core.BackgroundJobs;

public class DisableMultipleQueuedItemsAttribute : JobFilterAttribute, IClientFilter, IServerFilter
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FingerprintTimeout = TimeSpan.FromHours(1);

    public void OnCreating(CreatingContext filterContext)
    {
        if (!AddFingerprintIfNotExists(filterContext.Connection, filterContext.Job))
        {
            filterContext.Canceled = true;
        }
    }

    public void OnPerformed(PerformedContext filterContext)
    {
        if (filterContext.BackgroundJob.Job == null)
        {
            return;
        }

        RemoveFingerprint(filterContext.Connection, filterContext.BackgroundJob.Job);
    }

    private static bool AddFingerprintIfNotExists(IStorageConnection connection, Job job)
    {
        using (connection.AcquireDistributedLock(GetFingerprintLockKey(job), LockTimeout))
        {
            var fingerprint = connection.GetAllEntriesFromHash(GetFingerprintKey(job));

            if (fingerprint != null &&
                fingerprint.TryGetValue("Timestamp", out var value) &&
                DateTimeOffset.TryParse(value, null, DateTimeStyles.RoundtripKind, out var timestamp) &&
                DateTimeOffset.UtcNow <= timestamp.Add(FingerprintTimeout))
            {
                // Actual fingerprint found, returning.
                return false;
            }

            // Fingerprint does not exist, it is invalid (no `Timestamp` key),
            // or it is not actual (timeout expired).
            connection.SetRangeInHash(
                GetFingerprintKey(job),
                new Dictionary<string, string>
                {
                    { "Timestamp", DateTimeOffset.UtcNow.ToString("o") }
                }
            );

            return true;
        }
    }

    private static void RemoveFingerprint(IStorageConnection connection, Job job)
    {
        using (connection.AcquireDistributedLock(GetFingerprintLockKey(job), LockTimeout))
        using (var transaction = connection.CreateWriteTransaction())
        {
            transaction.RemoveHash(GetFingerprintKey(job));
            transaction.Commit();
        }
    }

    private static string GetFingerprintLockKey(Job job)
    {
        return $"{GetFingerprintKey(job)}:lock";
    }

    private static string GetFingerprintKey(Job job)
    {
        return $"fingerprint:{GetFingerprint(job)}";
    }

    private static string GetFingerprint(Job job)
    {
        if (job.Type == null || job.Method == null)
        {
            return string.Empty;
        }

        var payload = $"{job.Type.FullName}.{job.Method.Name}";

        var fingerprint = string.Concat(XxHash64.Hash(Encoding.UTF8.GetBytes(payload)).Select(x => x.ToString("X2")));

        return fingerprint;
    }

    void IClientFilter.OnCreated(CreatedContext filterContext)
    {
    }

    void IServerFilter.OnPerforming(PerformingContext filterContext)
    {
    }
}