using System;
using Newtonsoft.Json;

namespace BPS.Contracts.Queue
{
    public sealed class JobDto
    {
        [JsonProperty("jobId")]
        public string JobId { get; set; } = string.Empty;

        [JsonProperty("jobType")]
        public string JobType { get; set; } = string.Empty;

        [JsonProperty("priority")]
        public JobPriority Priority { get; set; } = JobPriority.Medium;

        [JsonProperty("status")]
        public JobStatus Status { get; set; } = JobStatus.Queued;

        [JsonProperty("payload")]
        public string Payload { get; set; } = string.Empty;

        [JsonProperty("createdAtUtc")]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [JsonProperty("startedAtUtc")]
        public DateTime? StartedAtUtc { get; set; }

        [JsonProperty("finishedAtUtc")]
        public DateTime? FinishedAtUtc { get; set; }

        [JsonProperty("lastError")]
        public string LastError { get; set; } = string.Empty;
    }
}
