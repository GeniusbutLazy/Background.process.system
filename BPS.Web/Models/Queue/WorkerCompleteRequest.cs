namespace BPS.Web.Models.Queue;

public sealed class WorkerCompleteRequest
{
    public JobStatus Status { get; set; } = JobStatus.Completed;
    public string Error { get; set; } = string.Empty;
}
