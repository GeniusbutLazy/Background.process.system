namespace BPS.Web.Models.Queue;

public sealed class JobDto
{
    public string JobId { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public JobPriority Priority { get; set; } = JobPriority.Medium;
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public string LastError { get; set; } = string.Empty;
}
