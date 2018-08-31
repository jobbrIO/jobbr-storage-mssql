using Jobbr.ComponentModel.JobStorage.Model;

namespace Jobbr.Storage.MsSql.Mapping
{
    public static class JobRunMapping
    {
        public static JobRun ToModel(this Entities.JobRun entity)
        {
            var jobrun = new JobRun
            {
                Id = entity.Id,
                ActualEndDateTimeUtc = entity.ActualEndDateTimeUtc,
                ActualStartDateTimeUtc = entity.ActualStartDateTimeUtc,
                EstimatedEndDateTimeUtc = entity.EstimatedEndDateTimeUtc,
                InstanceParameters = entity.InstanceParameters,
                JobParameters = entity.JobParameters,
                Pid = entity.Pid,
                PlannedStartDateTimeUtc = entity.PlannedStartDateTimeUtc,
                Progress = entity.Progress,
                State = entity.State
            };

            return jobrun;
        }

        public static Entities.JobRun ToEntity(this JobRun model)
        {
            var entity = new Entities.JobRun
            {
                Id = model.Id,
                ActualEndDateTimeUtc = model.ActualEndDateTimeUtc,
                ActualStartDateTimeUtc = model.ActualStartDateTimeUtc,
                EstimatedEndDateTimeUtc = model.EstimatedEndDateTimeUtc,
                InstanceParameters = model.InstanceParameters,
                JobParameters = model.JobParameters,
                Pid = model.Pid,
                PlannedStartDateTimeUtc = model.PlannedStartDateTimeUtc,
                Progress = model.Progress,
                State = model.State,
                
            };

            return entity;
        }
    }

    public static class JobMapping
    {
        public static Job ToModel(this Entities.Job entity)
        {
            var job = new Job
            {
                Id = entity.Id,
                CreatedDateTimeUtc = entity.CreatedDateTimeUtc,
                Parameters = entity.Parameters,
                Title = entity.Title,
                Type = entity.Type,
                UniqueName = entity.UniqueName,
                UpdatedDateTimeUtc = entity.UpdatedDateTimeUtc
            };

            return job;
        }

        public static Entities.Job ToEntity(this Job model)
        {
            var entity = new Entities.Job

            {
                Id = model.Id,
                CreatedDateTimeUtc = model.CreatedDateTimeUtc,
                Parameters = model.Parameters,
                Title = model.Title,
                Type = model.Type,
                UniqueName = model.UniqueName,
                UpdatedDateTimeUtc = model.UpdatedDateTimeUtc
            };

            return entity;
        }
    }
}
