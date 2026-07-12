namespace BPS.Web.Models.Queue;

public sealed class QueueConfiguration
{
    public int MaxConcurrentJobs { get; set; } = 2;
    public int MaxJobDurationSeconds { get; set; } = 120;
}
