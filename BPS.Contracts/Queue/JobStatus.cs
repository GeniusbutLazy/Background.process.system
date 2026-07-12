using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BPS.Contracts.Queue
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum JobStatus
    {
        Queued = 0,
        Running = 1,
        Completed = 2,
        Failed = 3,
        Stopped = 4
    }
}
