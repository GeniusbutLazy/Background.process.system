using Newtonsoft.Json;

namespace BPS.Contracts.Queue
{
    public sealed class QueueConfiguration
    {
        [JsonProperty("maxConcurrentJobs")]
        public int MaxConcurrentJobs { get; set; } = 2;

        [JsonProperty("maxJobDurationSeconds")]
        public int MaxJobDurationSeconds { get; set; } = 120;
    }
}
