using System;
using System.Runtime.Serialization;

namespace BPS.Service.Queue
{
    [DataContract]
    internal class QueueConfiguration
    {
        [DataMember]
        public int MaxConcurrentJobs { get; set; }

        [DataMember]
        public int MaxJobDurationSeconds { get; set; }
    }

    [DataContract]
    internal class JobDto
    {
        [DataMember]
        public string JobId { get; set; }

        [DataMember]
        public string JobType { get; set; }

        [DataMember]
        public string Priority { get; set; }

        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public string Payload { get; set; }

        [DataMember]
        public DateTime CreatedAtUtc { get; set; }

        [DataMember]
        public DateTime? StartedAtUtc { get; set; }

        [DataMember]
        public DateTime? FinishedAtUtc { get; set; }

        [DataMember]
        public string LastError { get; set; }
    }

    [DataContract]
    internal class WorkerCompleteRequest
    {
        [DataMember]
        public string Status { get; set; }

        [DataMember]
        public string Error { get; set; }
    }
}
