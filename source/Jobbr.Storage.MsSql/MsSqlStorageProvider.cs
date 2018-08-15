using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Jobbr.ComponentModel.JobStorage;
using Jobbr.ComponentModel.JobStorage.Model;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Dapper;

namespace Jobbr.Storage.MsSql
{
    public class MsSqlStorageProvider : IJobStorageProvider
    {
        private readonly JobbrMsSqlConfiguration _configuration;
        private readonly OrmLiteConnectionFactory _ormLiteConnectionFactory;

        public MsSqlStorageProvider(JobbrMsSqlConfiguration configuration)
        {
            this._configuration = configuration;
            _ormLiteConnectionFactory =
                new OrmLiteConnectionFactory(configuration.ConnectionString, configuration.DialectProvider);
        }

        public override string ToString()
        {
            return $"[{this.GetType().Name}, Schema: '{this._configuration.Schema}', Connection: '{this._configuration.ConnectionString}']";
        }

        public List<Job> GetJobs(int page = 1, int pageSize = 50)
        {
            return GetFromDb(c =>
            {
                return c.SelectLazy<Job>().Skip(page * (pageSize - 1)).Take(pageSize).OrderBy(k => k.CreatedDateTimeUtc);
            });
        }

        public void AddJob(Job job)
        {
            DoDbWorkload(connection => connection.Insert(job));
        }

        public void DeleteJob(long jobId)
        {
            DoDbWorkload(con => con.Delete<Job>(job => job.Id == jobId));
        }

        public JobRun GetLastJobRunByTriggerId(long jobId, long triggerId, DateTime utcNow)
        {
            var jobRun = this.GetScalarFromDb(con =>
            {
                return con.SelectLazy<JobRun>().FirstOrDefault(i => i.Id == jobId && i.ActualStartDateTimeUtc < utcNow);
            });

            if (jobRun == null)
            {
                return null;
            }

            var trigger =
                GetScalarFromDb(con => con.SelectLazy<Trigger>().FirstOrDefault(j => j.JobId == jobRun.Id));

            return new JobRun
            {
                Trigger = JobTriggerTriggerFactory.CreateTriggerFromDto(trigger),
                ActualEndDateTimeUtc = jobRun.ActualEndDateTimeUtc,
                EstimatedEndDateTimeUtc = jobRun.EstimatedEndDateTimeUtc,
                Id = jobRun.Id,
                Job = jobRun.Job,
                State = jobRun.State,
                ActualStartDateTimeUtc = jobRun.ActualStartDateTimeUtc,
                PlannedStartDateTimeUtc = jobRun.PlannedStartDateTimeUtc,
                InstanceParameters = jobRun.InstanceParameters,
                JobParameters = jobRun.JobParameters,
                Pid = jobRun.Pid,
                Progress = jobRun.Progress,
            };
        }

        public JobRun GetNextJobRunByTriggerId(long jobId, long triggerId, DateTime utcNow)
        {
            var jobRun = this.GetScalarFromDb(con => con.SelectLazy<JobRun>().Where(i => i.Trigger.Id == triggerId).OrderBy(p => p.PlannedStartDateTimeUtc).FirstOrDefault());
            return jobRun;
        }

        public PagedResult<JobRun> GetJobRuns(int page = 1, int pageSize = 50, string jobTypeFilter = null, string jobUniqueNameFilter = null,
            string query = null, params string[] sort)
        {
            AssertOnlyOneFilterIsActive(jobTypeFilter, jobUniqueNameFilter, query);

            var items = GetFromDb(con =>
            {
                var jobRuns = con.SelectLazy<JobRun>();
                jobRuns = QueryExtender.SortFilteredJobRuns(page, pageSize, jobTypeFilter, jobUniqueNameFilter, query, sort, jobRuns);

                return jobRuns.AsList();
            });

            return CreatePagedResult(page, pageSize, items);
        }

        public PagedResult<JobRun> GetJobRunsByJobId(int jobId, int page = 1, int pageSize = 50, params string[] sort)
        {
            var jobRuns = GetFromDb(con =>
            {
                var jobs = con.SelectLazy<JobRun>().Where(s => s.Job.Id == jobId);
                jobs = QueryExtender.SortJobRuns(jobs, sort).Skip(pageSize * (page - 1)).Take(pageSize);

                return jobs.AsList();
            });

            return CreatePagedResult(page, pageSize, jobRuns);
        }

        public PagedResult<JobRun> GetJobRunsByUserId(string userId, int page = 1, int pageSize = 50, string jobTypeFilter = null,
            string jobUniqueNameFilter = null, params string[] sort)
        {
            throw new NotImplementedException();
        }

        public PagedResult<JobRun> GetJobRunsByTriggerId(long jobId, long triggerId, int page = 1, int pageSize = 50, params string[] sort)
        {
            throw new NotImplementedException();
        }

        public PagedResult<JobRun> GetJobRunsByUserDisplayName(string userDisplayName, int page = 1, int pageSize = 50,
            string jobTypeFilter = null, string jobUniqueNameFilter = null, params string[] sort)
        {
            throw new NotImplementedException();
        }

        public PagedResult<JobRun> GetJobRunsByState(JobRunStates state, int page = 1, int pageSize = 50, string jobTypeFilter = null,
            string jobUniqueNameFilter = null, string query = null, params string[] sort)
        {
            throw new NotImplementedException();
        }

        public void AddJobRun(JobRun jobRun)
        {
            this.DoDbWorkload(con => con.Insert(jobRun));
        }

        public List<JobRun> GetJobRuns(int page = 0, int pageSize = 50)
        {
            return this.GetFromDb(con => con.SelectLazy<JobRun>().Skip(page * pageSize).Take(pageSize));
        }

        public void UpdateProgress(long jobRunId, double? progress)
        {
            this.DoDbWorkload(con => con.Update<JobRun>(new { Progress = progress }, p => p.Id == jobRunId));
        }

        public void Update(JobRun jobRun)
        {
            var fromDb = this.GetJobRunById(jobRun.Id);

            DoDbWorkload(con => con.Update(
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
                    State = jobRun.State.ToString()
                }, job => job.Id == jobRun.Id
            ));
        }

        public PagedResult<Job> GetJobs(int page = 1, int pageSize = 50, string jobTypeFilter = null, string jobUniqueNameFilter = null,
            string query = null, params string[] sort)
        {
            throw new NotImplementedException();
        }

        public Job GetJobById(long id)
        {
            return GetScalarFromDb(con => con.SingleById<Job>(id));
        }

        public Job GetJobByUniqueName(string identifier)
        {
            var sql = $"SELECT TOP 1 * FROM {this._configuration.Schema}.Jobs WHERE [UniqueName] = @UniqueName";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<Job>(sql, new { UniqueName = identifier }).FirstOrDefault();
            }
        }

        public JobRun GetJobRunById(long id)
        {
            return GetScalarFromDb(con => con.SingleById<JobRun>(id));
        }

        public List<JobRun> GetJobRunsByUserId(string userId, int page = 0, int pageSize = 50)
        {
            var sql = $"SELECT jr.* FROM {this._configuration.Schema}.JobRuns AS jr LEFT JOIN {this._configuration.Schema}.Triggers AS tr ON tr.Id = jr.TriggerId WHERE tr.UserId = @Id ORDER BY jr.PlannedStartDateTimeUtc ASC OFFSET {page * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new { Id = userId }).ToList();
            }
        }

        public List<JobRun> GetJobRunsByUserDisplayName(string userDisplayName, int page = 0, int pageSize = 50)
        {
            var sql = $"SELECT jr.* FROM {this._configuration.Schema}.JobRuns AS jr LEFT JOIN {this._configuration.Schema}.Triggers AS tr ON tr.Id = jr.TriggerId WHERE tr.UserDisplayName = @UserDisplayName ORDER BY jr.PlannedStartDateTimeUtc ASC OFFSET {page * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new { UserDisplayName = userDisplayName }).ToList();
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

        public void DeleteTrigger(long jobId, long triggerId)
        {
            throw new NotImplementedException();
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

        public List<JobRun> GetJobRunsByTriggerId(long jobId, long triggerId, int page = 0, int pageSize = 50)
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.JobRuns WHERE [TriggerId] = @TriggerId ORDER BY PlannedStartDateTimeUtc DESC OFFSET {page * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new { TriggerId = triggerId }).ToList();
            }
        }

        public List<JobRun> GetJobRunsByState(JobRunStates state, int page = 0, int pageSize = 50)
        {
            var sql = $"SELECT * FROM {this._configuration.Schema}.JobRuns WHERE [State] = @State ORDER BY PlannedStartDateTimeUtc ASC OFFSET {page * pageSize} ROWS FETCH NEXT {pageSize} ROWS ONLY";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.Query<JobRun>(sql, new { State = state.ToString() }).ToList();
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

        public PagedResult<JobTriggerBase> GetActiveTriggers(int page = 1, int pageSize = 50, string jobTypeFilter = null,
            string jobUniqueNameFilter = null, string query = null, params string[] sort)
        {
            throw new NotImplementedException();
        }

        public void DisableTrigger(long jobId, long triggerId)
        {
            var sql = $"UPDATE {this._configuration.Schema}.Triggers SET [IsActive] = @IsActive WHERE [Id] = @TriggerId";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, new { TriggerId = triggerId, IsActive = false });
            }
        }

        public void EnableTrigger(long jobId, long triggerId)
        {
            var sql = $"UPDATE {this._configuration.Schema}.Triggers SET [IsActive] = @IsActive WHERE [Id] = @TriggerId";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                connection.Execute(sql, new { TriggerId = triggerId, IsActive = true });
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
                using (var multi = connection.QueryMultiple(sql, new { JobId = jobId }))
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

        public PagedResult<JobTriggerBase> GetTriggersByJobId(long jobId, int page = 1, int pageSize = 50)
        {
            throw new NotImplementedException();
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

            var param = new { };

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

        private static PagedResult<T> CreatePagedResult<T>(int page, int pageSize, List<T> items)
        {
            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = items?.Count ?? 0
            };
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

        public long GetJobsCount()
        {
            var sql = $@"SELECT COUNT(*) FROM {this._configuration.Schema}.Jobs";

            using (var connection = new SqlConnection(this._configuration.ConnectionString))
            {
                return connection.ExecuteScalar<long>(sql);
            }
        }

        private static void AssertOnlyOneFilterIsActive(string jobTypeFilter, string jobUniqueNameFilter, string query)
        {
            var sum = IsNull(jobTypeFilter) + IsNull(jobUniqueNameFilter) + IsNull(query);

            if (sum > 1)
            {
                throw new InvalidOperationException("Only one filter is allowed");
            }

            int IsNull(string input)
            {
                return input == null ? 0 : 1;
            }
        }

        private List<T> GetFromDb<T>(Func<IDbConnection, IEnumerable<T>> dbWork)
        {
            using (var connection = _ormLiteConnectionFactory.Open())
            {
                var jobs = dbWork(connection);

                return jobs.AsList();
            }
        }

        private T GetScalarFromDb<T>(Func<IDbConnection, T> dbWork)
        {
            using (var connection = _ormLiteConnectionFactory.Open())
            {
                var scalarFromDb = dbWork(connection);

                return scalarFromDb;
            }
        }

        private void DoDbWorkload(Action<IDbConnection> dbWork)
        {
            using (var connection = _ormLiteConnectionFactory.Open())
            {
                dbWork(connection);
            }
        }
    }
}