using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BPS.Contracts.Queue
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum JobPriority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
}
