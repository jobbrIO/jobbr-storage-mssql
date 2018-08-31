using System;
using Jobbr.ComponentModel.JobStorage.Model;
using ServiceStack.DataAnnotations;

namespace Jobbr.Storage.MsSql.Entities
{
    [Alias("JobRuns")]
    public class JobRun
    {
        [AutoIncrement]
        public long Id { get; set; }
        public long JobId { get; set; }
        public long TriggerId { get; set; }
        public JobRunStates State { get; set; }
        public double? Progress { get; set; }
        public DateTime PlannedStartDateTimeUtc { get; set; }
        public DateTime? ActualStartDateTimeUtc { get; set; }
        public DateTime? ActualEndDateTimeUtc { get; set; }
        public DateTime? EstimatedEndDateTimeUtc { get; set; }
        public string JobParameters { get; set; }
        public string InstanceParameters { get; set; }
        public int? Pid { get; set; }
    }

    public class JobRunInfo
    {
        public long Id { get; set; }
        public long JobId { get; set; }
        public long TriggerId { get; set; }
        public Job Job { get; set; }
        public Trigger Trigger { get; set; }
        public JobRunStates State { get; set; }
        public double? Progress { get; set; }
        public DateTime PlannedStartDateTimeUtc { get; set; }
        public DateTime? ActualStartDateTimeUtc { get; set; }
        public DateTime? ActualEndDateTimeUtc { get; set; }
        public DateTime? EstimatedEndDateTimeUtc { get; set; }
        public string JobParameters { get; set; }
        public string InstanceParameters { get; set; }
        public int? Pid { get; set; }
    }
}