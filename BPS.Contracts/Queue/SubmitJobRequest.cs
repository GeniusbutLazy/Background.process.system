using Newtonsoft.Json;

namespace BPS.Contracts.Queue
{
    public sealed class SubmitJobRequest
    {
        [JsonProperty("jobType")]
        public string JobType { get; set; } = "DelayJob";

        [JsonProperty("priority")]
        public JobPriority Priority { get; set; } = JobPriority.Medium;

        [JsonProperty("payload")]
        public string Payload { get; set; } = "5";
    }
}
