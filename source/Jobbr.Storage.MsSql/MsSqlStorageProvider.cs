using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Jobbr.ComponentModel.JobStorage;
using Jobbr.ComponentModel.JobStorage.Model;
using Jobbr.Storage.MsSql.Entities;
using Jobbr.Storage.MsSql.Mapping;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Dapper;
using Job = Jobbr.ComponentModel.JobStorage.Model.Job;
using JobRun = Jobbr.ComponentModel.JobStorage.Model.JobRun;

namespace Jobbr.Storage.MsSql
{
    public class MsSqlStorageProvider : IJobStorageProvider
    {
        private readonly JobbrMsSqlConfiguration configuration;
        private readonly OrmLiteConnectionFactory ormLiteConnectionFactory;

        public MsSqlStorageProvider(JobbrMsSqlConfiguration configuration)
        {
            this.configuration = configuration;
            this.ormLiteConnectionFactory = new OrmLiteConnectionFactory(configuration.ConnectionString, configuration.DialectProvider);

            this.CreateTables();
        }

        private void CreateTables()
        {
            using (var session = this.ormLiteConnectionFactory.OpenDbConnection())
            {
                session.CreateTableIfNotExists<Entities.Job>();
                session.CreateTableIfNotExists<Entities.JobRun>();
                session.CreateTableIfNotExists<Entities.Trigger>();
            }
        }

        public override string ToString()
        {
            return $"[{this.GetType().Name}, Schema: '{this.configuration.Schema}', Connection: '{this.configuration.ConnectionString}']";
        }

        public List<Job> GetJobs(int page = 1, int pageSize = 50)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                var entities = connection.SelectLazy<Entities.Job>().Skip((page - 1) * pageSize).Take(pageSize).OrderBy(k => k.CreatedDateTimeUtc);

                return entities.Select(s => s.ToModel()).ToList();
            }
        }

        public void AddJob(Job job)
        {
            var entity = job.ToEntity();

            using (var session = this.ormLiteConnectionFactory.Open())
            {
                job.Id = session.Insert(entity, true);
            }
        }

        public void DeleteJob(long jobId)
        {
            this.Db(con => con.Delete<Entities.Job>(job => job.Id == jobId));
        }

        public JobRun GetLastJobRunByTriggerId(long jobId, long triggerId, DateTime utcNow)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                return connection.Select<JobRunInfo>(
                        connection.From<Entities.JobRun>()
                            .Join<Entities.JobRun, Entities.Trigger>((jr, t) => jr.TriggerId == t.Id)
                            .Join<Entities.JobRun, Entities.Job>((jr, j) => jr.TriggerId == j.Id)
                            .Where(p => p.TriggerId == triggerId && p.JobId == jobId)
                            .Where(p => p.PlannedStartDateTimeUtc >= utcNow)
                            .OrderBy(o => o.PlannedStartDateTimeUtc))
                            .Take(1)
                    .Select(s => s.ToModel())
                    .FirstOrDefault();
            }
        }

        public JobRun GetNextJobRunByTriggerId(long jobId, long triggerId, DateTime utcNow)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                return connection.Select<JobRunInfo>(
                        connection.From<Entities.JobRun>()
                            .Join<Entities.JobRun, Entities.Trigger>((jr, t) => jr.TriggerId == t.Id)
                            .Join<Entities.JobRun, Entities.Job>((jr, j) => jr.TriggerId == j.Id)
                            .Where(p => p.TriggerId == triggerId && p.JobId == jobId)
                            .Where(p => p.State == JobRunStates.Scheduled)
                            .OrderBy(o => o.PlannedStartDateTimeUtc))
                    .Take(1)
                    .Select(s => s.ToModel())
                    .FirstOrDefault();
            }
        }

        public PagedResult<JobRun> GetJobRuns(int page = 1, int pageSize = 50, string jobTypeFilter = null, string jobUniqueNameFilter = null,
            string query = null, params string[] sort)
        {
            AssertOnlyOneFilterIsActive(jobTypeFilter, jobUniqueNameFilter, query);

            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                SqlExpression<Entities.JobRun> sqlExpression = connection.From<Entities.JobRun>()
                    .Join<Entities.JobRun, Entities.Trigger>((jr, t) => jr.TriggerId == t.Id)
                    .Join<Entities.JobRun, Entities.Job>((jr, j) => jr.TriggerId == j.Id);

                if (jobTypeFilter != null)
                {
                    sqlExpression = sqlExpression
                        .And<Job>(p => p.Type == jobTypeFilter);
                }
                else if (jobUniqueNameFilter != null)
                {
                    sqlExpression = sqlExpression
                        .And<Job>(p => p.UniqueName == jobUniqueNameFilter);
                }
                else if (query != null)
                {
                    sqlExpression = sqlExpression
                        .And<Job>(p => p.UniqueName.Contains(query) || p.Type.Contains(query) || p.Title.Contains(query));
                }

                var rows = connection.Select<JobRunInfo>(sqlExpression)
                    .Select(s => s.ToModel())
                    .ToList();

                var count = connection.Count(sqlExpression);

                return new PagedResult<JobRun>
                {
                    Items = rows,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = (int)count,
                };

            }
            throw new NotImplementedException();

            //var items = this.GetFromDb(con =>
            //{
            //    var jobRuns = con.SelectLazy<JobRun>();
            //    jobRuns = QueryExtender.SortFilteredJobRuns(jobRuns, page, pageSize, jobTypeFilter, jobUniqueNameFilter, query, sort);

            //    return jobRuns.AsList();
            //});

            //return CreatePagedResult(page, pageSize, items);
        }

        public PagedResult<JobRun> GetJobRunsByJobId(int jobId, int page = 1, int pageSize = 50, params string[] sort)
        {
            var jobRuns = this.GetFromDb(con =>
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
            return this.GetJobRunsByCriteria<Trigger>(crit => crit.UserId == userId,
                s => s.JobId, page, pageSize, jobTypeFilter, jobUniqueNameFilter, sort);
        }

        public PagedResult<JobRun> GetJobRunsByTriggerId(long jobId, long triggerId, int page = 1, int pageSize = 50, params string[] sort)
        {
            return this.GetJobRunsByCriteria<Trigger>(crit => crit.Id == triggerId,
                s => s.JobId, page, pageSize, null, null, sort);
        }

        public PagedResult<JobRun> GetJobRunsByUserDisplayName(string userDisplayName, int page = 1, int pageSize = 50,
            string jobTypeFilter = null, string jobUniqueNameFilter = null, params string[] sort)
        {
            return this.GetJobRunsByCriteria<Trigger>(crit => crit.UserDisplayName == userDisplayName,
                s => s.JobId, page, pageSize, jobTypeFilter, jobUniqueNameFilter, sort);
        }

        public PagedResult<JobRun> GetJobRunsByState(JobRunStates state, int page = 1, int pageSize = 50, string jobTypeFilter = null,
            string jobUniqueNameFilter = null, string query = null, params string[] sort)
        {
            throw new NotImplementedException();
        }

        public void AddJobRun(JobRun jobRun)
        {
            var entity = jobRun.ToEntity();

            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                jobRun.Id = connection.Insert(entity, true);
            }
        }

        public List<JobRun> GetJobRuns(int page = 1, int pageSize = 50)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                return connection.Select<JobRunInfo>(
                        connection.From<Entities.JobRun>()
                            .Join<Entities.JobRun, Entities.Trigger>((jr, t) => jr.TriggerId == t.Id)
                            .Join<Entities.JobRun, Entities.Job>((jr, j) => jr.TriggerId == j.Id)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize))
                    .Select(s => s.ToModel())
                    .ToList();
            }
        }

        public void UpdateProgress(long jobRunId, double? progress)
        {
            this.Db(con => con.Update<Entities.JobRun>(new { Progress = progress }, p => p.Id == jobRunId));
        }

        public void Update(JobRun jobRun)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                var entity = jobRun.ToEntity();

                connection.Update(entity);
            }
        }

        public PagedResult<Job> GetJobs(int page = 1, int pageSize = 50, string jobTypeFilter = null, string jobUniqueNameFilter = null,
            string query = null, params string[] sort)
        {
            AssertOnlyOneFilterIsActive(jobTypeFilter, jobUniqueNameFilter, query);

            var items = this.GetFromDb(con =>
            {
                var jobs = con.SelectLazy<Job>();
                jobs = QueryExtender.SortFilteredJobs(jobs, page, pageSize, jobTypeFilter, jobUniqueNameFilter, query, sort);

                return jobs.AsList();
            });

            return CreatePagedResult(page, pageSize, items);
        }

        public Job GetJobById(long id)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                return connection.SingleById<Entities.Job>(id).ToModel();
            }
        }

        public Job GetJobByUniqueName(string identifier)
        {
            return this.GetScalarFromDb(con => con.Single<Entities.Job>(new { UniqueName = identifier })).ToModel();
        }

        public JobRun GetJobRunById(long id)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                return connection.Select<JobRunInfo>(
                        connection.From<Entities.JobRun>()
                            .Join<Entities.JobRun, Entities.Trigger>((jr, t) => jr.TriggerId == t.Id)
                            .Join<Entities.JobRun, Entities.Job>((jr, j) => jr.TriggerId == j.Id)
                            .Where(p => p.Id == id))
                    .Select(s => s.ToModel()).FirstOrDefault();
            }
        }

        public List<JobRun> GetJobRunsByUserId(string userId, int page = 1, int pageSize = 50)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                var rows = connection.Select<JobRunInfo>(
                    connection.From<Entities.JobRun>()
                        .Join<Entities.JobRun, Entities.Trigger>((jr, t) => jr.TriggerId == t.Id)
                        .And<Entities.Trigger>(p => p.UserId == userId)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize));

                return rows.Select(s => s.ToModel()).ToList();
            }
        }

        public List<JobRun> GetJobRunsByUserDisplayName(string userDisplayName, int page = 1, int pageSize = 50)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                var rows = connection.Select<JobRunInfo>(
                    connection.From<Entities.JobRun>()
                        .Join<Entities.JobRun, Entities.Trigger>((jr, t) => jr.TriggerId == t.Id)
                        .And<Entities.Trigger>(p => p.UserDisplayName == userDisplayName)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize));

                return rows.Select(s => s.ToModel()).ToList();
            }
        }

        public void Update(Job job)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                var entity = job.ToEntity();
                entity.UpdatedDateTimeUtc = DateTime.UtcNow;

                connection.Update<Entities.Job>(entity);
            }
        }

        public void DeleteTrigger(long jobId, long triggerId)
        {
            this.Db(con => con.DeleteById<Trigger>(triggerId));
        }

        public void Update(long jobId, InstantTrigger trigger)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                var entity = trigger.ToEntity();

                connection.Update(entity);
            }
        }

        public void Update(long jobId, ScheduledTrigger trigger)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                var entity = trigger.ToEntity();

                connection.Update(entity);
            }
        }

        public void Update(long jobId, RecurringTrigger trigger)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                var entity = trigger.ToEntity();

                connection.Update(entity);
            }
        }

        public List<JobRun> GetJobRunsByTriggerId(long jobId, long triggerId, int page = 1, int pageSize = 50)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                return connection.Select<JobRunInfo>(
                    connection.From<Entities.JobRun>()
                        .Join<Entities.JobRun, Entities.Trigger>((jr, t) => jr.TriggerId == t.Id)
                        .Join<Entities.JobRun, Entities.Job>((jr, j) => jr.TriggerId == j.Id)
                        .And<Entities.Job>(p => p.Id == jobId)
                        .And<Entities.Trigger>(p => p.Id == triggerId)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize))
                        .Select(s => s.ToModel())
                        .ToList();


                //return connection.SelectLazy<Entities.JobRun>()
                //                 .Skip((page - 1) * pageSize)
                //                 .Take(pageSize)
                //                 .Where(p => p.TriggerId == triggerId && p.JobId == jobId)
                //                 .ToList()
                //                 .Select(s => s.ToModel())
                //                 .ToList();
            }
        }

        public List<JobRun> GetJobRunsByState(JobRunStates state, int page = 1, int pageSize = 50)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                return connection.Select<JobRunInfo>(
                        connection.From<Entities.JobRun>()
                            .Join<Entities.JobRun, Entities.Trigger>((jr, t) => jr.TriggerId == t.Id)
                            .Join<Entities.JobRun, Entities.Job>((jr, j) => jr.TriggerId == j.Id)
                            .Where(p => p.State == state)
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize))
                    .Select(s => s.ToModel())
                    .ToList();

                //return connection.SelectLazy<Entities.JobRun>()
                //                 .Skip((page - 1) * pageSize)
                //                 .Take(pageSize)
                //                 .Where(p => p.State == state)
                //                 .ToList()
                //                 .Select(s => s.ToModel())
                //                 .ToList();
            }
        }

        public void AddTrigger(long jobId, InstantTrigger trigger)
        {
            trigger.JobId = jobId;

            this.InsertTrigger(trigger);
        }

        public void AddTrigger(long jobId, ScheduledTrigger trigger)
        {
            trigger.JobId = jobId;

            this.InsertTrigger(trigger);
        }

        public void AddTrigger(long jobId, RecurringTrigger trigger)
        {
            trigger.JobId = jobId;

            this.InsertTrigger(trigger);
        }

        public PagedResult<JobTriggerBase> GetActiveTriggers(int page = 1, int pageSize = 50,
            string jobTypeFilter = null,
            string jobUniqueNameFilter = null, string query = null, params string[] sort)
        {
            var activeTrigger = this.GetFromDb(con => con.Select<Trigger>().Where(t => t.IsActive));
            var trigger = activeTrigger.Select(JobTriggerTriggerFactory.CreateTriggerFromDto);

            // TODO: JobTypeFilter, JobUniqueNameFilter, Query, Sort implement

            return CreatePagedResult(page, pageSize, trigger.ToList());
        }

        public void DisableTrigger(long jobId, long triggerId)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                connection.Update<Entities.Trigger>(new { IsActive = false }, p => p.Id == triggerId);
            }
        }

        public void EnableTrigger(long jobId, long triggerId)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                connection.Update<Entities.Trigger>(new { IsActive = true }, p => p.Id == triggerId);
            }
        }

        public List<JobTriggerBase> GetTriggersByJobId(long jobId)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                return connection.Select<Entities.Trigger>(p => p.JobId == jobId)
                    .ToList()
                    .Select(s => s.ToModel())
                    .ToList();
            }
        }

        public JobTriggerBase GetTriggerById(long jobId, long triggerId)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                return connection.Select<Entities.Trigger>(p => p.JobId == jobId && p.Id == triggerId)
                    .First()
                    .ToModel();
            }
        }

        public PagedResult<JobTriggerBase> GetTriggersByJobId(long jobId, int page = 1, int pageSize = 50)
        {
            var triggers = this.GetFromDb(con => con.Select<Trigger>().Where(t => t.JobId == jobId))
                .Skip(pageSize * (page - 1))
                .Take(pageSize)
                .Select(JobTriggerTriggerFactory.CreateTriggerFromDto)
                .ToList();

            return CreatePagedResult(page, pageSize, triggers);
        }

        public List<JobTriggerBase> GetActiveTriggers()
        {
            return this.GetFromDb(con => con.Select<Entities.Trigger>().Where(t => t.IsActive))
                .Select(s => s.ToModel())
                .ToList();
        }

        private PagedResult<JobRun> GetJobRunsByCriteria<TCriterion>(
            Func<TCriterion, bool> filter,
            Func<TCriterion, long> idSelector,
            int page = 1,
            int pageSize = 50,
            string jobTypeFilter = null,
            string jobUniqueNameFilter = null,
            params string[] sort)
        {
            var jobRuns = this.GetFromDb(con =>
            {
                // First get the corresponding JobRun from the TriggerId, which is defined in the trigger
                var triggers = con.SelectLazy<TCriterion>().Where(filter);
                var correspondingJobRunIds = triggers.Select(idSelector).Distinct().AsList();

                // Now get every jobrun
                var jobs = con.SelectLazy<JobRun>().Where(p => correspondingJobRunIds.Contains(p.Id))
                    .Skip(pageSize * (page - 1))
                    .Take(pageSize);
                jobs = QueryExtender.SortFilteredJobRuns(jobs, page, pageSize, jobTypeFilter, jobUniqueNameFilter, null, sort);

                return jobs.AsList();
            });

            return CreatePagedResult(page, pageSize, jobRuns);
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
            return this.GetScalarFromDb(con => con.Count<Entities.Job>());
        }

        private void InsertTrigger(InstantTrigger trigger)
        {
            var entity = trigger.ToEntity();

            if (entity.CreatedDateTimeUtc == default(DateTime))
            {
                entity.CreatedDateTimeUtc = DateTime.UtcNow;
            }

            using (var connection = this.ormLiteConnectionFactory.OpenDbConnection())
            {
                trigger.Id = connection.Insert(entity, true);
            }
        }

        private void InsertTrigger(ScheduledTrigger trigger)
        {
            var entity = trigger.ToEntity();

            if (entity.CreatedDateTimeUtc == default(DateTime))
            {
                entity.CreatedDateTimeUtc = DateTime.UtcNow;
            }

            using (var connection = this.ormLiteConnectionFactory.OpenDbConnection())
            {
                trigger.Id = connection.Insert(entity, true);
            }
        }

        private void InsertTrigger(RecurringTrigger trigger)
        {
            var entity = trigger.ToEntity();

            if (entity.CreatedDateTimeUtc == default(DateTime))
            {
                entity.CreatedDateTimeUtc = DateTime.UtcNow;
            }

            using (var connection = this.ormLiteConnectionFactory.OpenDbConnection())
            {
                trigger.Id = connection.Insert(entity, true);
            }
        }

        private void InsertTrigger(JobTriggerBase trigger, string type, string definition = "", DateTime? startDateTimeUtc = null, DateTime? endDateTimeUtc = null, int DelayedMinutes = 0, bool noParallelExecution = false)
        {
            var dateTimeUtcNow = DateTime.UtcNow;

            var sql =
                $@"INSERT INTO {this.configuration.Schema}.Triggers([JobId],[TriggerType],[Definition],[StartDateTimeUtc],[EndDateTimeUtc],[DelayedMinutes],[IsActive],[UserId],[UserDisplayName],[Parameters],[Comment],[CreatedDateTimeUtc],[NoParallelExecution])
                  VALUES (@JobId,@TriggerType,@Definition,@StartDateTimeUtc,@EndDateTimeUtc,@DelayedMinutes,@IsActive,@UserId,@UserDisplayName,@Parameters,@Comment,@UtcNow,@NoParallelExecution)
                  SELECT CAST(SCOPE_IDENTITY() as int)";

            using (var connection = new SqlConnection(this.configuration.ConnectionString))
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

        private List<JobTriggerBase> ExecuteSelectTriggerQuery(string sql, object param)
        {
            using (var connection = new SqlConnection(this.configuration.ConnectionString))
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
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                var jobs = dbWork(connection);

                return jobs.AsList();
            }
        }

        private T GetScalarFromDb<T>(Func<IDbConnection, T> dbWork)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                var scalarFromDb = dbWork(connection);

                return scalarFromDb;
            }
        }

        private void Db(Action<IDbConnection> dbWork)
        {
            using (var connection = this.ormLiteConnectionFactory.Open())
            {
                dbWork(connection);
            }
        }
    }
}