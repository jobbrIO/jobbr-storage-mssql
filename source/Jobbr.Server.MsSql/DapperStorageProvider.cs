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
    public class DapperStorageProvider : IJobStorageProvider
    {
        private readonly JobbrMsSqlConfiguration _configuration;

        public DapperStorageProvider(JobbrMsSqlConfiguration configuration)
        {
            _configuration = configuration;
        }

        public override string ToString()
        {
            return $"[{this.GetType().Name}, Schema: '{this._configuration.Schema}', Connection: '{this._configuration.ConnectionString}']";
        }

        public List<Job> GetJobs()
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.Jobs";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                var jobs = connection.Query<Job>(sql);

                return jobs.ToList();
            }
        }

        public long AddJob(Job job)
        {
            var sql =
                $@"INSERT INTO {this._configuration.Schema}.Jobs ([UniqueName],[Title],[Type],[Parameters],[CreatedDateTimeUtc]) VALUES (@UniqueName, @Title, @Type, @Parameters, @UtcNow)
                          SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return
                    connection.Query<int>(sql,
                        new {job.UniqueName, job.Title, job.Type, job.Parameters, DateTime.UtcNow,}).Single();
            }
        }

        public JobRun GetLastJobRunByTriggerId(long triggerId, DateTime utcNow)
        {
            var sql =
                $"SELECT TOP 1 * FROM {this._configuration.Schema}.JobRuns WHERE [TriggerId] = @TriggerId AND [ActualStartDateTimeUtc] < @DateTimeNowUtc ORDER BY [ActualStartDateTimeUtc] DESC";

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

        public JobRun GetNextJobRunByTriggerId(long triggerId, DateTime utcNow)
        {
            var sql =
                $"SELECT * FROM {this._configuration.Schema}.JobRuns WHERE [TriggerId] = @TriggerId AND PlannedStartDateTimeUtc >= @DateTimeNowUtc AND State = @State ORDER BY [PlannedStartDateTimeUtc] ASC";

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

        public int AddJobRun(JobRun jobRun)
        {
            var sql =
                $@"INSERT INTO {this._configuration.Schema}.JobRuns ([JobId],[TriggerId],[UniqueId],[JobParameters],[InstanceParameters],[PlannedStartDateTimeUtc],[State])
                          VALUES (@JobId,@TriggerId,@UniqueId,@JobParameters,@InstanceParameters,@PlannedStartDateTimeUtc,@State)
                          SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                var jobRunObject =
                    new
                    {
                        jobRun.JobId,
                        jobRun.TriggerId,
                        jobRun.UniqueId,
                        jobRun.JobParameters,
                        jobRun.InstanceParameters,
                        jobRun.PlannedStartDateTimeUtc,
                        State = jobRun.State.ToString()
                    };

                var id = connection.Query<int>(sql, jobRunObject).Single();

                return id;
            }
        }

        public List<JobRun> GetJobRuns()
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.JobRuns";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql).ToList();
            }
        }

        public bool UpdateProgress(long jobRunId, double? progress)
        {
            var sql = $@"UPDATE {this._configuration.Schema}.{"JobRuns"} SET [Progress] = @Progress WHERE [Id] = @Id";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, new {Id = jobRunId, Progress = progress});

                return true;
            }
        }

        public bool Update(JobRun jobRun)
        {
            var fromDb = this.GetJobRunById(jobRun.Id);

            if (fromDb == null)
            {
                return false;
            }

            var sql = $@"UPDATE {this._configuration.Schema}.{"JobRuns"} SET
                                        [JobParameters] = @JobParameters,
                                        [InstanceParameters] = @InstanceParameters,
                                        [PlannedStartDateTimeUtc] = @PlannedStartDateTimeUtc,
                                        [ActualStartDateTimeUtc] = @ActualStartDateTimeUtc,
                                        [EstimatedEndDateTimeUtc] = @EstimatedEndDateTimeUtc,
                                        [ActualEndDateTimeUtc] = @ActualEndDateTimeUtc,
                                        [Progress] = @Progress,
                                        [State] = @State,
                                        [Pid] = @Pid,
                                        [WorkingDir] = @WorkingDir,
                                        [TempDir] = @TempDir
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

                return true;
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

        public List<JobRun> GetJobRunsByUserId(long userId)
        {
            var sql =
                string.Format(
                    "SELECT jr.* FROM {0}.JobRuns AS jr LEFT JOIN {0}.Triggers AS tr ON tr.Id = jr.TriggerId WHERE tr.UserId = @Id",
                    this._configuration.Schema);

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new {Id = userId}).ToList();
            }
        }

        public List<JobRun> GetJobRunsByUserName(string userName)
        {
            var sql =
                string.Format(
                    "SELECT jr.* FROM {0}.JobRuns AS jr LEFT JOIN {0}.Triggers AS tr ON tr.Id = jr.TriggerId WHERE tr.UserName = @UserName",
                    this._configuration.Schema);

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new {UserName = userName}).ToList();
            }
        }

        public bool Update(Job job)
        {
            var fromDb = this.GetJobById(job.Id);

            if (fromDb == null)
            {
                return false;
            }

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

                return true;
            }
        }

        public bool Update(InstantTrigger trigger)
        {
            return this.UpdateTrigger(trigger, delayedInMinutes: trigger.DelayedMinutes);
        }

        public bool Update(ScheduledTrigger trigger)
        {
            return this.UpdateTrigger(trigger, startDateTimeUtc: trigger.StartDateTimeUtc,
                endDateTimeUtc: trigger.StartDateTimeUtc);
        }

        public bool Update(RecurringTrigger trigger)
        {
            return this.UpdateTrigger(trigger, trigger.Definition);
        }

        public List<JobRun> GetJobRunsByTriggerId(long triggerId)
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.JobRuns WHERE [TriggerId] = @TriggerId";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new {TriggerId = triggerId}).ToList();
            }
        }

        public bool CheckParallelExecution(long triggerId)
        {
            var sql =
                $"SELECT * FROM {this._configuration.Schema}.JobRuns WHERE [TriggerId] = @TriggerId AND [State] IN ('{JobRunStates.Collecting}','{JobRunStates.Connected}','{JobRunStates.Finishing}','{JobRunStates.Initializing}','{JobRunStates.Preparing}','{JobRunStates.Processing}','{JobRunStates.Scheduled}','{JobRunStates.Started}','{JobRunStates.Starting}')";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new {TriggerId = triggerId}).Any() == false;
            }
        }

        public List<JobRun> GetJobRunsByState(JobRunStates state)
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.JobRuns WHERE [State] = @State";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new {State = state.ToString()}).ToList();
            }
        }

        public long AddTrigger(InstantTrigger trigger)
        {
            
            return this.InsertTrigger(trigger, TriggerType.Instant, delayedInMinutes: trigger.DelayedMinutes);
        }

        public long AddTrigger(ScheduledTrigger trigger)
        {
            return this.InsertTrigger(trigger, TriggerType.Scheduled, startDateTimeUtc: trigger.StartDateTimeUtc,
                endDateTimeUtc: trigger.StartDateTimeUtc);
        }

        public long AddTrigger(RecurringTrigger trigger)
        {
            return this.InsertTrigger(trigger, TriggerType.Recurring, trigger.Definition,
                noParallelExecution: trigger.NoParallelExecution);
        }

        public bool DisableTrigger(long triggerId)
        {
            var sql = $"UPDATE {this._configuration.Schema}.Triggers SET [IsActive] = @IsActive WHERE [Id] = @TriggerId";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, new {TriggerId = triggerId, IsActive = false});

                return true;
            }
        }

        public bool EnableTrigger(long triggerId)
        {
            var sql = $"UPDATE {this._configuration.Schema}.Triggers SET [IsActive] = @IsActive WHERE [Id] = @TriggerId";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, new {TriggerId = triggerId, IsActive = true});

                return true;
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

        public JobTriggerBase GetTriggerById(long triggerId)
        {
            var sql = string.Format(
                @"SELECT * FROM {0}.Triggers where TriggerType = '{1}' AND Id = @Id
                      SELECT * FROM {0}.Triggers where TriggerType = '{2}' AND Id = @Id
                      SELECT * FROM {0}.Triggers where TriggerType = '{3}' AND Id = @Id",
                this._configuration.Schema,
                TriggerType.Instant,
                TriggerType.Recurring,
                TriggerType.Scheduled);

            var param = new {Id = triggerId};

            return this.ExecuteSelectTriggerQuery(sql, param).FirstOrDefault();
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

        private long InsertTrigger(JobTriggerBase trigger, string type, string definition = "",
            DateTime? startDateTimeUtc = null, DateTime? endDateTimeUtc = null, int delayedInMinutes = 0,
            bool noParallelExecution = false)
        {
            var dateTimeUtcNow = DateTime.UtcNow;

            var sql =
                $@"INSERT INTO {this._configuration.Schema}.Triggers([JobId],[TriggerType],[Definition],[StartDateTimeUtc],[EndDateTimeUtc],[DelayedInMinutes],[IsActive],[UserId],[UserName],[UserDisplayName],[Parameters],[Comment],[CreatedDateTimeUtc],[NoParallelExecution])
                  VALUES (@JobId,@TriggerType,@Definition,@StartDateTimeUtc,@EndDateTimeUtc,@DelayedInMinutes,1,@UserId,@UserName,@UserDisplayName,@Parameters,@Comment,@UtcNow,@NoParallelExecution)
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
                        DelayedInMinutes = delayedInMinutes,
                        trigger.IsActive,
                        trigger.UserId,
                        trigger.UserName,
                        trigger.UserDisplayName,
                        trigger.Parameters,
                        trigger.Comment,
                        UtcNow = dateTimeUtcNow,
                        NoParallelExecution = noParallelExecution
                    };

                var id = connection.Query<int>(sql, triggerObject).Single();
                trigger.CreatedDateTimeUtc = dateTimeUtcNow;
                trigger.Id = id;

                return id;
            }
        }

        private bool UpdateTrigger(JobTriggerBase trigger, string definition = "", DateTime? startDateTimeUtc = null,
            DateTime? endDateTimeUtc = null, int delayedInMinutes = 0)
        {
            var sql = $@"UPDATE {this._configuration.Schema}.[Triggers]
                  SET [Definition] = @Definition
                     ,[StartDateTimeUtc] = @StartDateTimeUtc
                     ,[EndDateTimeUtc] = @EndDateTimeUtc
                     ,[DelayedInMinutes] = @DelayedInMinutes
                     ,[IsActive] = @IsActive
                     ,[UserId] = @UserId
                     ,[UserName] = @UserName
                     ,[UserDisplayName] = @UserDisplayName
                     ,[Parameters] = @Parameters
                     ,[Comment] = @Comment
                 WHERE Id = @Id";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                var triggerObject =
                    new
                    {
                        trigger.Id,
                        Definition = definition,
                        StartDateTimeUtc = startDateTimeUtc,
                        EndDateTimeUtc = endDateTimeUtc,
                        DelayedInMinutes = delayedInMinutes,
                        trigger.IsActive,
                        trigger.UserId,
                        trigger.UserName,
                        trigger.UserDisplayName,
                        trigger.Parameters,
                        trigger.Comment,
                    };

                connection.Execute(sql, triggerObject);

                return true;
            }
        }
    }
}