using System.Collections.Concurrent;
using BPS.Web.Models.Queue;

namespace BPS.Web.Services.Queue;

public sealed class InMemoryQueueStore : IQueueStore
{
    private readonly ConcurrentDictionary<string, JobDto> _jobs = new();
    private QueueConfiguration _configuration = new();
    private readonly object _configurationLock = new();

    public JobDto Submit(SubmitJobRequest request)
    {
        var job = new JobDto
        {
            JobId = Guid.NewGuid().ToString("N"),
            JobType = string.IsNullOrWhiteSpace(request.JobType) ? "DelayJob" : request.JobType,
            Priority = request.Priority,
            Status = JobStatus.Queued,
            Payload = request.Payload ?? string.Empty,
            CreatedAtUtc = DateTime.UtcNow
        };

        _jobs.TryAdd(job.JobId, job);
        return Clone(job);
    }

    public IReadOnlyCollection<JobDto> List(JobPriority? priority)
    {
        var jobs = _jobs.Values
            .Where(x => !priority.HasValue || x.Priority == priority.Value)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAtUtc)
            .Select(Clone)
            .ToList();

        return jobs;
    }

    public JobDto? Get(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return null;
        }

        return Clone(job);
    }

    public bool TryUpdateJobStatus(string jobId, JobStatus status)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return false;
        }

        lock (job)
        {
            if (!CanTransition(job.Status, status))
            {
                return false;
            }

            ApplyStatus(job, status, string.Empty);
            return true;
        }
    }

    public bool TryUpdateConfiguration(QueueConfiguration configuration)
    {
        if (configuration.MaxConcurrentJobs <= 0 || configuration.MaxJobDurationSeconds <= 0)
        {
            return false;
        }

        lock (_configurationLock)
        {
            _configuration = new QueueConfiguration
            {
                MaxConcurrentJobs = configuration.MaxConcurrentJobs,
                MaxJobDurationSeconds = configuration.MaxJobDurationSeconds
            };
        }

        return true;
    }

    public QueueConfiguration GetConfiguration()
    {
        lock (_configurationLock)
        {
            return new QueueConfiguration
            {
                MaxConcurrentJobs = _configuration.MaxConcurrentJobs,
                MaxJobDurationSeconds = _configuration.MaxJobDurationSeconds
            };
        }
    }

    public JobDto? TryClaimNext()
    {
        foreach (var candidate in _jobs.Values
                     .Where(x => x.Status == JobStatus.Queued)
                     .OrderByDescending(x => x.Priority)
                     .ThenBy(x => x.CreatedAtUtc))
        {
            lock (candidate)
            {
                if (candidate.Status != JobStatus.Queued)
                {
                    continue;
                }

                ApplyStatus(candidate, JobStatus.Running, string.Empty);
                return Clone(candidate);
            }
        }

        return null;
    }

    public bool TryComplete(string jobId, JobStatus status, string error)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            return false;
        }

        if (status != JobStatus.Completed && status != JobStatus.Failed && status != JobStatus.Stopped)
        {
            return false;
        }

        lock (job)
        {
            if (job.Status != JobStatus.Running)
            {
                return false;
            }

            ApplyStatus(job, status, error ?? string.Empty);
            return true;
        }
    }

    private static bool CanTransition(JobStatus current, JobStatus target)
    {
        return (current, target) switch
        {
            (JobStatus.Queued, JobStatus.Running) => true,
            (JobStatus.Queued, JobStatus.Stopped) => true,
            (JobStatus.Running, JobStatus.Stopped) => true,
            (JobStatus.Stopped, JobStatus.Queued) => true,
            (JobStatus.Failed, JobStatus.Queued) => true,
            _ => false
        };
    }

    private static void ApplyStatus(JobDto job, JobStatus status, string error)
    {
        job.Status = status;

        if (status == JobStatus.Running)
        {
            job.StartedAtUtc = DateTime.UtcNow;
            job.FinishedAtUtc = null;
            job.LastError = string.Empty;
            return;
        }

        if (status == JobStatus.Queued)
        {
            job.StartedAtUtc = null;
            job.FinishedAtUtc = null;
            job.LastError = string.Empty;
            return;
        }

        job.FinishedAtUtc = DateTime.UtcNow;
        job.LastError = error;
    }

    private static JobDto Clone(JobDto source)
    {
        return new JobDto
        {
            JobId = source.JobId,
            JobType = source.JobType,
            Priority = source.Priority,
            Status = source.Status,
            Payload = source.Payload,
            CreatedAtUtc = source.CreatedAtUtc,
            StartedAtUtc = source.StartedAtUtc,
            FinishedAtUtc = source.FinishedAtUtc,
            LastError = source.LastError
        };
    }
}
