using Newtonsoft.Json;

namespace BPS.Contracts.Queue
{
    public sealed class WorkerCompleteRequest
    {
        [JsonProperty("status")]
        public JobStatus Status { get; set; } = JobStatus.Completed;

        [JsonProperty("error")]
        public string Error { get; set; } = string.Empty;
    }
}
