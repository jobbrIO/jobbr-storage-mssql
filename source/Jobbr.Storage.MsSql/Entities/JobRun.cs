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
        [StringLength(int.MaxValue)]
        public string JobParameters { get; set; }
        [StringLength(int.MaxValue)]
        public string InstanceParameters { get; set; }
        public int? Pid { get; set; }
    }
}