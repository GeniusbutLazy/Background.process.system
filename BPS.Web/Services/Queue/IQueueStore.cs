using BPS.Contracts.Queue;

namespace BPS.Web.Services.Queue;

public interface IQueueStore
{
    JobDto Submit(SubmitJobRequest request);
    IReadOnlyCollection<JobDto> List(JobPriority? priority);
    JobDto? Get(string jobId);
    bool TryUpdateJobStatus(string jobId, JobStatus status);
    bool TryUpdateConfiguration(QueueConfiguration configuration);
    QueueConfiguration GetConfiguration();
    JobDto? TryClaimNext();
    bool TryComplete(string jobId, JobStatus status, string error);
}
