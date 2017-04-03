using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Jobbr.ComponentModel.JobStorage;
using Jobbr.ComponentModel.JobStorage.Model;

namespace Jobbr.Server.MsSql
{
    /// <summary>
    /// The jobbr dapper provider to store jobserver repository, queue and status information
    /// </summary>
    public class MsSqlStorageProvider : IJobStorageProvider
    {
        private readonly JobbrMsSqlConfiguration _configuration;

        public MsSqlStorageProvider(JobbrMsSqlConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override string ToString()
        {
            return $"[{this.GetType().Name}, Schema: '{this._configuration.Schema}', Connection: '{this._configuration.ConnectionString}']";
        }

        public List<Job> GetJobs(int page = 0, int pageSize = 50)
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.Jobs ORDER BY CreatedDateTimeUtc ASC OFFSET {page} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                var jobs = connection.Query<Job>(sql);

                return jobs.ToList();
            }
        }

        public void AddJob(Job job)
        {
            var sql = $@"INSERT INTO {this._configuration.Schema}.Jobs ([UniqueName],[Title],[Type],[Parameters],[CreatedDateTimeUtc]) VALUES (@UniqueName, @Title, @Type, @Parameters, @UtcNow) SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                var id = connection.Query<int>(sql, new {job.UniqueName, job.Title, job.Type, job.Parameters, DateTime.UtcNow,}).Single();
                job.Id = id;
            }
        }

        public JobRun GetLastJobRunByTriggerId(long jobId, long triggerId, DateTime utcNow)
        {
            var sql = $"SELECT TOP 1 * FROM {this._configuration.Schema}.JobRuns WHERE [TriggerId] = @TriggerId AND [ActualStartDateTimeUtc] < @DateTimeNowUtc ORDER BY [ActualStartDateTimeUtc] DESC";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                var jobRuns = connection.Query<JobRun>(sql, new
                {
                    TriggerId = triggerId,
                    DateTimeNowUtc = utcNow
                }).ToList();

                return jobRuns.Any() ? jobRuns.FirstOrDefault() : null;
            }
        }

        public JobRun GetNextJobRunByTriggerId(long jobId, long triggerId, DateTime utcNow)
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.JobRuns WHERE [TriggerId] = @TriggerId AND PlannedStartDateTimeUtc >= @DateTimeNowUtc AND State = @State ORDER BY [PlannedStartDateTimeUtc] ASC";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                var jobRuns =
                    connection.Query<JobRun>(sql,
                        new
                        {
                            TriggerId = triggerId,
                            DateTimeNowUtc = utcNow,
                            State = JobRunStates.Scheduled.ToString()
                        }).ToList();

                return jobRuns.Any() ? jobRuns.FirstOrDefault() : null;
            }
        }

        public void AddJobRun(JobRun jobRun)
        {
            var sql =
                $@"INSERT INTO {this._configuration.Schema}.JobRuns ([JobId],[TriggerId],[JobParameters],[InstanceParameters],[PlannedStartDateTimeUtc],[ActualStartDateTimeUtc],[State])
                          VALUES (@JobId,@TriggerId,@JobParameters,@InstanceParameters,@PlannedStartDateTimeUtc,@ActualStartDateTimeUtc,@State)
                          SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                var jobRunObject =
                    new
                    {
                        jobRun.JobId,
                        jobRun.TriggerId,
                        jobRun.JobParameters,
                        jobRun.InstanceParameters,
                        jobRun.PlannedStartDateTimeUtc,
                        State = jobRun.State.ToString(),
                        jobRun.ActualStartDateTimeUtc
                    };

                var id = connection.Query<int>(sql, jobRunObject).Single();

                jobRun.Id = id;
            }
        }

        public List<JobRun> GetJobRuns(long page = 0, long pageSize = 50)
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.JobRuns ORDER BY PlannedStartDateTimeUtc DESC OFFSET {page} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql).ToList();
            }
        }

        public void UpdateProgress(long jobRunId, double? progress)
        {
            var sql = $@"UPDATE {this._configuration.Schema}.{"JobRuns"} SET [Progress] = @Progress WHERE [Id] = @Id";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, new {Id = jobRunId, Progress = progress});
            }
        }

        public void Update(JobRun jobRun)
        {
            var fromDb = this.GetJobRunById(jobRun.Id);

            var sql = $@"UPDATE {this._configuration.Schema}.{"JobRuns"} SET
                                        [JobParameters] = @JobParameters,
                                        [InstanceParameters] = @InstanceParameters,
                                        [PlannedStartDateTimeUtc] = @PlannedStartDateTimeUtc,
                                        [ActualStartDateTimeUtc] = @ActualStartDateTimeUtc,
                                        [EstimatedEndDateTimeUtc] = @EstimatedEndDateTimeUtc,
                                        [ActualEndDateTimeUtc] = @ActualEndDateTimeUtc,
                                        [Progress] = @Progress,
                                        [State] = @State,
                                        [Pid] = @Pid
                                    WHERE [Id] = @Id";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(
                    sql,
                    new
                    {
                        jobRun.Id,
                        jobRun.JobParameters,
                        jobRun.InstanceParameters,
                        jobRun.PlannedStartDateTimeUtc,
                        jobRun.Progress,
                        jobRun.Pid,
                        ActualStartDateTimeUtc = jobRun.ActualStartDateTimeUtc ?? fromDb.ActualStartDateTimeUtc,
                        EstimatedEndDateTimeUtc = jobRun.EstimatedEndDateTimeUtc ?? fromDb.EstimatedEndDateTimeUtc,
                        ActualEndDateTimeUtc = jobRun.ActualEndDateTimeUtc ?? fromDb.ActualEndDateTimeUtc,
                        State = jobRun.State.ToString(),
                    });
            }
        }

        public Job GetJobById(long id)
        {
            var sql = $"SELECT TOP 1 * FROM {this._configuration.Schema}.Jobs WHERE [Id] = @Id";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<Job>(sql, new {Id = id}).FirstOrDefault();
            }
        }

        public Job GetJobByUniqueName(string identifier)
        {
            var sql = $"SELECT TOP 1 * FROM {this._configuration.Schema}.Jobs WHERE [UniqueName] = @UniqueName";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<Job>(sql, new {UniqueName = identifier}).FirstOrDefault();
            }
        }

        public JobRun GetJobRunById(long id)
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.JobRuns WHERE [Id] = @Id";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new {Id = id}).FirstOrDefault();
            }
        }

        public List<JobRun> GetJobRunsByUserId(string userId, long page = 0, long pageSize = 50)
        {
            var sql = $"SELECT jr.* FROM {this._configuration.Schema}.JobRuns AS jr LEFT JOIN {this._configuration.Schema}.Triggers AS tr ON tr.Id = jr.TriggerId WHERE tr.UserId = @Id ORDER BY jr.PlannedStartDateTimeUtc ASC OFFSET {page} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new {Id = userId}).ToList();
            }
        }

        public List<JobRun> GetJobRunsByUserDisplayName(string userDisplayName, long page = 0, long pageSize = 50)
        {
            var sql = $"SELECT jr.* FROM {this._configuration.Schema}.JobRuns AS jr LEFT JOIN {this._configuration.Schema}.Triggers AS tr ON tr.Id = jr.TriggerId WHERE tr.UserDisplayName = @UserDisplayName ORDER BY jr.PlannedStartDateTimeUtc ASC OFFSET {page} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new {UserDisplayName = userDisplayName }).ToList();
            }
        }

        public void Update(Job job)
        {
            var sql = $@"UPDATE {this._configuration.Schema}.{"Jobs"} SET
                                        [UniqueName] = @UniqueName,
                                        [Title] = @Title,
                                        [Type] = @Type,
                                        [Parameters] = @Parameters,
                                        [UpdatedDateTimeUtc] = @UtcNow
                                    WHERE [Id] = @Id";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(
                    sql,
                    new
                    {
                        job.Id,
                        job.UniqueName,
                        job.Title,
                        job.Type,
                        job.Parameters,
                        DateTime.UtcNow
                    });
            }
        }

        public void Update(long jobId, InstantTrigger trigger)
        {
            var sql = $@"UPDATE {this._configuration.Schema}.[Triggers]
                  SET [IsActive] = @IsActive
                     ,[UserId] = @UserId
                     ,[UserDisplayName] = @UserDisplayName
                     ,[Parameters] = @Parameters
                     ,[Comment] = @Comment
                     ,[DelayedMinutes] = @DelayedMinutes
                 WHERE Id = @Id";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, trigger);
            }
        }

        public void Update(long jobId, ScheduledTrigger trigger)
        {
            var sql = $@"UPDATE {this._configuration.Schema}.[Triggers]
                  SET [IsActive] = @IsActive
                     ,[UserId] = @UserId
                     ,[UserDisplayName] = @UserDisplayName
                     ,[Parameters] = @Parameters
                     ,[Comment] = @Comment
                     ,[StartDateTimeUtc] = @StartDateTimeUtc
                 WHERE Id = @Id";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, trigger);
            }
        }

        public void Update(long jobId, RecurringTrigger trigger)
        {
            var sql = $@"UPDATE {this._configuration.Schema}.[Triggers]
                  SET [IsActive] = @IsActive
                     ,[UserId] = @UserId
                     ,[UserDisplayName] = @UserDisplayName
                     ,[Parameters] = @Parameters
                     ,[Comment] = @Comment
                     ,[StartDateTimeUtc] = @StartDateTimeUtc
                     ,[EndDateTimeUtc] = @EndDateTimeUtc
                     ,[Definition] = @Definition
                     ,[NoParallelExecution] = @NoParallelExecution
                 WHERE Id = @Id";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, trigger);
            }
        }

        public List<JobRun> GetJobRunsByTriggerId(long jobId, long triggerId, long page = 0, long pageSize = 50)
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.JobRuns WHERE [TriggerId] = @TriggerId ORDER BY PlannedStartDateTimeUtc DESC OFFSET {page} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new {TriggerId = triggerId}).ToList();
            }
        }

        public List<JobRun> GetJobRunsByState(JobRunStates state, long page = 0, long pageSize = 50)
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.JobRuns WHERE [State] = @State ORDER BY PlannedStartDateTimeUtc ASC OFFSET {page} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new {State = state.ToString()}).ToList();
            }
        }

        public void AddTrigger(long jobId, InstantTrigger trigger)
        {
            trigger.JobId = jobId;
            this.InsertTrigger(trigger, TriggerType.Instant, DelayedMinutes: trigger.DelayedMinutes);
        }

        public void AddTrigger(long jobId, ScheduledTrigger trigger)
        {
            trigger.JobId = jobId;
            this.InsertTrigger(trigger, TriggerType.Scheduled, startDateTimeUtc: trigger.StartDateTimeUtc, endDateTimeUtc: trigger.StartDateTimeUtc);
        }

        public void AddTrigger(long jobId, RecurringTrigger trigger)
        {
            trigger.JobId = jobId;
            this.InsertTrigger(trigger, TriggerType.Recurring, trigger.Definition, noParallelExecution: trigger.NoParallelExecution);
        }

        public void DisableTrigger(long jobId, long triggerId)
        {
            var sql = $"UPDATE {this._configuration.Schema}.Triggers SET [IsActive] = @IsActive WHERE [Id] = @TriggerId";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, new {TriggerId = triggerId, IsActive = false});
            }
        }

        public void EnableTrigger(long jobId, long triggerId)
        {
            var sql = $"UPDATE {this._configuration.Schema}.Triggers SET [IsActive] = @IsActive WHERE [Id] = @TriggerId";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, new {TriggerId = triggerId, IsActive = true});
            }
        }

        public List<JobTriggerBase> GetTriggersByJobId(long jobId)
        {
            var sql = string.Format(
                @"SELECT * FROM {0}.Triggers where TriggerType = '{1}' AND JobId = @JobId
                  SELECT * FROM {0}.Triggers where TriggerType = '{2}' AND JobId = @JobId
                  SELECT * FROM {0}.Triggers where TriggerType = '{3}' AND JobId = @JobId",
                this._configuration.Schema,
                TriggerType.Instant,
                TriggerType.Recurring,
                TriggerType.Scheduled);


            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                using (var multi = connection.QueryMultiple(sql, new {JobId = jobId}))
                {
                    var instantTriggers = multi.Read<InstantTrigger>().ToList();
                    var cronTriggers = multi.Read<RecurringTrigger>().ToList();
                    var dateTimeTriggers = multi.Read<ScheduledTrigger>().ToList();

                    var result = new List<JobTriggerBase>();

                    result.AddRange(instantTriggers);
                    result.AddRange(cronTriggers);
                    result.AddRange(dateTimeTriggers);

                    return result.OrderBy(t => t.Id).ToList();
                }
            }
        }

        public JobTriggerBase GetTriggerById(long jobId, long triggerId)
        {
            var sql = $@"SELECT * FROM {this._configuration.Schema}.Triggers WHERE Id = @Id";
            var param = new { Id = triggerId };

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                using (var reader = connection.ExecuteReader(sql, param))
                {
                    var instantTriggerParser = reader.GetRowParser<JobTriggerBase>(typeof(InstantTrigger));
                    var scheduledTriggerParser = reader.GetRowParser<JobTriggerBase>(typeof(ScheduledTrigger));
                    var recurringTriggerParser = reader.GetRowParser<JobTriggerBase>(typeof(RecurringTrigger));

                    var triggerTypeColumnIndex = reader.GetOrdinal("TriggerType");

                    JobTriggerBase trigger = null;

                    if (reader.Read())
                    {
                        var triggerType = reader.GetString(triggerTypeColumnIndex);
                        
                        switch (triggerType)
                        {
                            case TriggerType.Instant:
                                trigger = instantTriggerParser(reader);
                                break;

                            case TriggerType.Scheduled:
                                trigger = scheduledTriggerParser(reader);
                                break;

                            case TriggerType.Recurring:
                                trigger = recurringTriggerParser(reader);
                                break;
                        }
                    }

                    return trigger;
                }
            }
        }

        public List<JobTriggerBase> GetActiveTriggers()
        {
            var sql = string.Format(
                @"SELECT * FROM {0}.Triggers where TriggerType = '{1}' AND IsActive = 1
                  SELECT * FROM {0}.Triggers where TriggerType = '{2}' AND IsActive = 1
                  SELECT * FROM {0}.Triggers where TriggerType = '{3}' AND IsActive = 1",
                this._configuration.Schema,
                TriggerType.Instant,
                TriggerType.Recurring,
                TriggerType.Scheduled);

            var param = new {};

            return this.ExecuteSelectTriggerQuery(sql, param);
        }

        private List<JobTriggerBase> ExecuteSelectTriggerQuery(string sql, object param)
        {
            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                using (var multi = connection.QueryMultiple(sql, param))
                {
                    var instantTriggers = multi.Read<InstantTrigger>().ToList();
                    var recurringTriggers = multi.Read<RecurringTrigger>().ToList();
                    var scheduledTriggers = multi.Read<ScheduledTrigger>().ToList();

                    var result = new List<JobTriggerBase>();

                    result.AddRange(instantTriggers);
                    result.AddRange(recurringTriggers);
                    result.AddRange(scheduledTriggers);

                    return result.ToList();
                }
            }
        }

        private void InsertTrigger(JobTriggerBase trigger, string type, string definition = "", DateTime? startDateTimeUtc = null, DateTime? endDateTimeUtc = null, int DelayedMinutes = 0, bool noParallelExecution = false)
        {
            var dateTimeUtcNow = DateTime.UtcNow;

            var sql =
                $@"INSERT INTO {this._configuration.Schema}.Triggers([JobId],[TriggerType],[Definition],[StartDateTimeUtc],[EndDateTimeUtc],[DelayedMinutes],[IsActive],[UserId],[UserDisplayName],[Parameters],[Comment],[CreatedDateTimeUtc],[NoParallelExecution])
                  VALUES (@JobId,@TriggerType,@Definition,@StartDateTimeUtc,@EndDateTimeUtc,@DelayedMinutes,@IsActive,@UserId,@UserDisplayName,@Parameters,@Comment,@UtcNow,@NoParallelExecution)
                  SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                var triggerObject =
                    new
                    {
                        trigger.JobId,
                        TriggerType = type,
                        Definition = definition,
                        StartDateTimeUtc = startDateTimeUtc,
                        EndDateTimeUtc = endDateTimeUtc,
                        DelayedMinutes,
                        trigger.IsActive,
                        trigger.UserId,
                        trigger.UserDisplayName,
                        trigger.Parameters,
                        trigger.Comment,
                        UtcNow = dateTimeUtcNow,
                        NoParallelExecution = noParallelExecution
                    };

                var id = connection.Query<int>(sql, triggerObject).Single();
                trigger.CreatedDateTimeUtc = dateTimeUtcNow;
                trigger.Id = id;
            }
        }

        public bool IsAvailable()
        {
            try
            {
                this.GetJobs(0, 1);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}