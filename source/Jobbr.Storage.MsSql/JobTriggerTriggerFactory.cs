using System;
using Jobbr.ComponentModel.JobStorage.Model;

namespace Jobbr.Storage.MsSql
{
    /// <summary>
    /// Factory to create specialized triggers
    /// </summary>
    public static class JobTriggerTriggerFactory
    {
        public static JobTriggerBase CreateTriggerFromDto(Trigger trigger)
        {
            if (trigger == null)
            {
                return null;
            }

            switch (trigger.TriggerType)
            {
                case TriggerType.Instant:
                    return new InstantTrigger
                    {
                        Id = trigger.Id,
                        Comment = trigger.Comment,
                        CreatedDateTimeUtc = trigger.CreatedDateTimeUtc,
                        DelayedMinutes = trigger.DelayedMinutes,
                        IsActive = trigger.IsActive,
                        JobId = trigger.JobId,
                        Parameters = trigger.Parameters,
                        UserDisplayName = trigger.UserDisplayName,
                        UserId = trigger.UserId
                    };
                case TriggerType.Recurring:
                    return new RecurringTrigger
                    {
                        Comment = trigger.Comment,
                        CreatedDateTimeUtc = trigger.CreatedDateTimeUtc,
                        Definition = trigger.Defintion,
                        Id = trigger.Id,
                        UserId = trigger.UserId,
                        EndDateTimeUtc = trigger.EndDateTimeUtc,
                        IsActive = trigger.IsActive,
                        JobId = trigger.JobId,
                        NoParallelExecution = trigger.NoParallelExecution,
                        Parameters = trigger.Parameters
                    };
                case TriggerType.Scheduled:
                    return new ScheduledTrigger
                    {
                        Comment = trigger.Comment,
                        CreatedDateTimeUtc = trigger.CreatedDateTimeUtc,
                        Id = trigger.Id,
                        IsActive = trigger.IsActive,
                        JobId = trigger.JobId,
                        Parameters = trigger.Parameters,
                        StartDateTimeUtc = trigger.StartDateTimeUtc,
                        UserId = trigger.UserId,
                        UserDisplayName = trigger.UserDisplayName
                    };
                default:
                    throw new NotSupportedException("Unknown Type");
            }
        }
    }
}