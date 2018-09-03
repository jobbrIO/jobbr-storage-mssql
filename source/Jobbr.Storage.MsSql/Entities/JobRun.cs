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

    public class JobRunInfo
    {
        public long Id { get; set; }

        public long JobId { get; set; }
        public string JobUniqueName { get; set; }
        public string JobTitle { get; set; }
        public string JobParameters { get; set; }
        public string JobType { get; set; }
        public DateTime? JobUpdatedDateTimeUtc { get; set; }
        public DateTime? JobCreatedDateTimeUtc { get; set; }

        public long TriggerId { get; set; }
        public TriggerType TriggerType { get; set; }
        public bool TriggerIsActive { get; set; }
        public string TriggerUserId { get; set; }
        public string TriggerUserDisplayName { get; set; }
        public string TriggerParameters { get; set; }
        public string TriggerComment { get; set; }
        public DateTime TriggerCreatedDateTimeUtc { get; set; }
        public DateTime? TriggerStartDateTimeUtc { get; set; }
        public DateTime? TriggerEndDateTimeUtc { get; set; }
        public string TriggerDefinition { get; set; }
        public bool TriggerNoParallelExecution { get; set; }
        public int TriggerDelayedMinutes { get; set; }

        public JobRunStates State { get; set; }
        public double? Progress { get; set; }
        public DateTime PlannedStartDateTimeUtc { get; set; }
        public DateTime? ActualStartDateTimeUtc { get; set; }
        public DateTime? ActualEndDateTimeUtc { get; set; }
        public DateTime? EstimatedEndDateTimeUtc { get; set; }
        
        public string InstanceParameters { get; set; }
        public int? Pid { get; set; }
    }
}