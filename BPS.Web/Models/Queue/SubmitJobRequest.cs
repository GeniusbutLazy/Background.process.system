using System.Text.Json.Serialization;

namespace BPS.Web.Models.Queue;

public sealed class SubmitJobRequest
{

    public string JobType { get; set; } = "DelayJob";
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public JobPriority Priority { get; set; } = JobPriority.Medium;
    public string Payload { get; set; } = "5";
}
