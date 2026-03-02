using System.Globalization;
using System.IO.Hashing;
using System.Text;
using ecommerce.Core.Extensions;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

#pragma warning disable CS0618

namespace ecommerce.Core.BackgroundJobs;

public class DisableConcurrentExecutionWithRetryAttribute : JobFilterAttribute, IElectStateFilter, IApplyStateFilter
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(1);

    private readonly int _timeoutInSeconds;

    private readonly string? _resource;

    public int RetryInSeconds { get; set; } = 60;

    public int MaxRetryAttempts { get; set; }

    public DisableConcurrentExecutionWithRetryAttribute(int timeoutInSeconds, string? resource = null)
    {
        if (timeoutInSeconds < 0) throw new ArgumentException("Timeout argument value should be greater than zero.");

        _timeoutInSeconds = timeoutInSeconds;
        _resource = resource;
    }

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState.Name != ProcessingState.StateName || context.BackgroundJob.Job == null)
        {
            return;
        }

        var timeout = TimeSpan.FromSeconds(_timeoutInSeconds);

        try
        {
            using (context.Connection.AcquireDistributedLock(GetFingerprintLockKey(context.BackgroundJob.Job), LockTimeout))
            {
                var fingerprintKey = GetFingerprintKey(context.BackgroundJob.Job);
                var fingerprint = context.Connection.GetAllEntriesFromHash(fingerprintKey);

                var blockedByJobId = fingerprint?.GetValueOrDefault("JobId");

                if (fingerprint != null &&
                    blockedByJobId != context.BackgroundJob.Id &&
                    fingerprint.TryGetValue("Timestamp", out var value) &&
                    DateTimeOffset.TryParse(value, null, DateTimeStyles.RoundtripKind, out var timestamp) &&
                    DateTimeOffset.UtcNow <= timestamp.Add(timeout))
                {
                    var currentAttempt = context.GetJobParameter<int>("ConcurrentExecutionRetryAttempt") + 1;
                    context.SetJobParameter("ConcurrentExecutionRetryAttempt", currentAttempt);

                    context.CandidateState = MaxRetryAttempts == 0 || currentAttempt <= MaxRetryAttempts
                        ? CreateScheduledState(currentAttempt, blockedByJobId)
                        : CreateDeletedState(blockedByJobId);

                    return;
                }

                context.Connection.SetRangeInHash(
                    fingerprintKey,
                    new Dictionary<string, string>
                    {
                        { "Timestamp", DateTimeOffset.UtcNow.ToString("o") },
                        { "JobId", context.BackgroundJob.Id }
                    }
                );
            }
        }
        catch (DistributedLockTimeoutException)
        {
            context.CandidateState = new ScheduledState(TimeSpan.FromSeconds(RetryInSeconds))
            {
                Reason = "Couldn't acquire a distributed lock for mutex: timeout exceeded"
            };
        }
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.OldStateName != ProcessingState.StateName || context.BackgroundJob.Job == null)
        {
            return;
        }

        RemoveFingerprint(context.Connection, context.BackgroundJob.Job);
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
    }

    private void RemoveFingerprint(IStorageConnection connection, Job job)
    {
        using (connection.AcquireDistributedLock(GetFingerprintLockKey(job), LockTimeout))
        using (var transaction = connection.CreateWriteTransaction())
        {
            transaction.RemoveHash(GetFingerprintKey(job));
            transaction.Commit();
        }
    }

    private string GetFingerprintLockKey(Job job)
    {
        return $"{GetFingerprintKey(job)}:lock";
    }

    private string GetFingerprintKey(Job job)
    {
        return $"fingerprint:{GetFingerprint(job)}";
    }

    private string GetFingerprint(Job job)
    {
        var parameters = string.Empty;
        if (!string.IsNullOrEmpty(_resource))
        {
            var args = job.Args.ToArray();
            parameters = args.Length > 1
                ? string.Format(_resource, args)
                : args.FirstOrDefault().ToKeyValueString().Aggregate(_resource, (current, arg) => current.Replace($"{{{arg.Key}}}", arg.Value?.ToString()));
        }

        if (job.Type == null || job.Method == null)
        {
            return string.Empty;
        }

        var payload = $"{job.Type.FullName}.{job.Method.Name}.{parameters}";

        var fingerprint = string.Concat(XxHash64.Hash(Encoding.UTF8.GetBytes(payload)).Select(x => x.ToString("X2")));

        return fingerprint;
    }

    private static DeletedState CreateDeletedState(string? blockedByJobId)
    {
        return new DeletedState
        {
            Reason = $"Execution was blocked by background job with id \"{blockedByJobId}\" and all attempts exhausted."
        };
    }

    private IState CreateScheduledState(int currentAttempt, string? blockedByJobId)
    {
        var reason = $"Execution is blocked by background job with id \"{blockedByJobId}\", retry attempt: {currentAttempt}";

        if (MaxRetryAttempts > 0)
        {
            reason += $"/{MaxRetryAttempts}";
        }

        return new ScheduledState(TimeSpan.FromSeconds(RetryInSeconds))
        {
            Reason = reason
        };
    }
}