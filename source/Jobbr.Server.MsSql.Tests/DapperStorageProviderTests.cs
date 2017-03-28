using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Jobbr.ComponentModel.JobStorage.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.MsSql.Tests
{
    [TestClass]
    public class DapperStorageProviderTests
    {
        private SqlConnection _sqlConnection;
        private DapperStorageProvider _storageProvider;
        private LocalDb _localDb;

        [TestInitialize]
        public void Initialize()
        {
            _localDb = new LocalDb();
            _sqlConnection = _localDb.OpenConnection();
            _sqlConnection.Open();

            var sqlStatements = SqlHelper.SplitSqlStatements(File.ReadAllText("CreateSchemaAndTables.sql")).ToList();

            foreach (var statement in sqlStatements)
            {
                var command = new SqlCommand(statement, _sqlConnection);
                command.ExecuteNonQuery();
            }

            _storageProvider = new DapperStorageProvider(new JobbrMsSqlConfiguration
            {
                ConnectionString = _localDb.ConnectionStringName,
                Schema = "Jobbr"
            });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _sqlConnection.Close();
        }

        [TestMethod]
        public void GivenEmptyDatabase_WhenAddingJob_IdIsSet()
        {
            var job = new Job
            {
                UniqueName = "testjob",
                Type = "Jobs.Test"
            };

            _storageProvider.AddJob(job);

            Assert.AreNotEqual(0, job.Id);
        }

        [TestMethod]
        public void GivenJob_WhenQueryingById_IsReturned()
        {
            var job = new Job
            {
                UniqueName = "testjob",
                Type = "Jobs.Test"
            };

            _storageProvider.AddJob(job);

            var job2 = _storageProvider.GetJobById(job.Id);

            Assert.AreEqual(job.Id, job2.Id);
            Assert.AreEqual("testjob", job2.UniqueName);
            Assert.AreEqual("Jobs.Test", job2.Type);
        }

        [TestMethod]
        public void GivenJob_WhenQueryingByUniqueName_IsReturned()
        {
            var job = new Job
            {
                UniqueName = "testjob",
                Type = "Jobs.Test"
            };

            _storageProvider.AddJob(job);

            var job2 = _storageProvider.GetJobByUniqueName(job.UniqueName);

            Assert.AreEqual(job.Id, job2.Id);
            Assert.AreEqual("testjob", job2.UniqueName);
            Assert.AreEqual("Jobs.Test", job2.Type);
        }

        [TestMethod]
        public void GivenTwoJobs_WhenQueryingPaged_ResultIsPaged()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };
            var job2 = new Job { UniqueName = "testjob2", Type = "Jobs.Test2", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);
            _storageProvider.AddJob(job2);

            var jobs = _storageProvider.GetJobs(0, 1);

            Assert.AreEqual(1, jobs.Count);
            Assert.AreEqual(job1.Id, jobs[0].Id);
        }

        [TestMethod]
        public void GivenSomeTriggers_WhenQueryingForActiveTriggers_AllActiveTriggersAreReturned()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };
            var job2 = new Job { UniqueName = "testjob2", Type = "Jobs.Test2", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);
            _storageProvider.AddJob(job2);

            var trigger1 = new InstantTrigger { IsActive = false };
            var trigger2 = new InstantTrigger { IsActive = true };
            var trigger3 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);
            _storageProvider.AddTrigger(job1.Id, trigger2);
            _storageProvider.AddTrigger(job2.Id, trigger3);

            var activeTriggers = _storageProvider.GetActiveTriggers();

            Assert.AreEqual(2, activeTriggers.Count);
        }

        [TestMethod]
        public void GivenJobRun_WhenQueryingById_IsReturned()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJobRun(jobRun1);

            var jobRun2 = _storageProvider.GetJobRunById(jobRun1.Id);

            Assert.AreEqual(jobRun1.Id, jobRun2.Id);
        }

        [TestMethod]
        public void GivenTwoJobRuns_WhenQueryingPaged_ResultIsPaged()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);

            var jobRuns = _storageProvider.GetJobRuns(0, 1);

            Assert.AreEqual(1, jobRuns.Count);

            jobRuns = _storageProvider.GetJobRuns(0, 2);

            Assert.AreEqual(2, jobRuns.Count);
        }

        [TestMethod]
        public void GivenTwoJobRuns_WhenQueryingForSpecificState_OnlyThoseJobRunsAreReturned()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Failed };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);
            _storageProvider.AddJobRun(jobRun3);

            var jobRuns = _storageProvider.GetJobRunsByState(JobRunStates.Failed);

            Assert.AreEqual(1, jobRuns.Count);
        }

        [TestMethod]
        public void GivenThreeJobRuns_WhenQueryingForSpecificStatePaged_ResultIsPaged()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Failed };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);
            _storageProvider.AddJobRun(jobRun3);

            var jobRuns = _storageProvider.GetJobRunsByState(JobRunStates.Completed, 0, 1);

            Assert.AreEqual(1, jobRuns.Count);

            jobRuns = _storageProvider.GetJobRunsByState(JobRunStates.Completed, 0, 2);

            Assert.AreEqual(2, jobRuns.Count);
        }

        [TestMethod]
        public void GivenThreeJobRuns_WhenQueryingByTrigger_AllJobRunsOfTriggerAreReturned()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Failed };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);
            _storageProvider.AddJobRun(jobRun3);

            var jobRuns = _storageProvider.GetJobRunsByTriggerId(job1.Id, trigger1.Id);

            Assert.AreEqual(3, jobRuns.Count);
        }

        [TestMethod]
        public void GivenThreeJobRuns_WhenQueryingByTriggerPaged_ResultIsPaged()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Failed };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);
            _storageProvider.AddJobRun(jobRun3);

            var jobRuns = _storageProvider.GetJobRunsByTriggerId(job1.Id, trigger1.Id, 0, 2);

            Assert.AreEqual(2, jobRuns.Count);
        }

        [TestMethod]
        public void GivenThreeJobRunsOfChefkoch_WhenQueryingByUserDisplayName_ReturnsOnlyJobRunsOfThatUser()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true, UserDisplayName = "chefkoch" };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);
            _storageProvider.AddJobRun(jobRun3);

            var jobRuns = _storageProvider.GetJobRunsByUserDisplayName("chefkoch");

            Assert.AreEqual(3, jobRuns.Count);
        }

        [TestMethod]
        public void GivenThreeJobRunsOfChefkoch_WhenQueryingByUserDisplayNamePaged_ResultIsPaged()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true, UserDisplayName = "chefkoch" };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);
            _storageProvider.AddJobRun(jobRun3);

            var jobRuns = _storageProvider.GetJobRunsByUserDisplayName("chefkoch", 0, 2);

            Assert.AreEqual(2, jobRuns.Count);
        }

        [TestMethod]
        public void GivenThreeJobRunsOfozu_WhenQueryingByUserId_ReturnsOnlyJobRunsOfozu()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true, UserId = "ozu" };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);
            _storageProvider.AddJobRun(jobRun3);

            var jobRuns = _storageProvider.GetJobRunsByUserId("ozu");

            Assert.AreEqual(3, jobRuns.Count);
        }

        [TestMethod]
        public void GivenThreeJobRunsOfozu_WhenQueryingByUserIdPaged_ResultIsPaged()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true, UserId = "ozu" };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);
            _storageProvider.AddJobRun(jobRun3);

            var jobRuns = _storageProvider.GetJobRunsByUserId("ozu", 0, 2);

            Assert.AreEqual(2, jobRuns.Count);
        }

        [TestMethod]
        public void GivenEnabledTrigger_WhenDisabling_TriggerIsDisabled()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            _storageProvider.DisableTrigger(job1.Id, trigger1.Id);

            var triggerFromDb = _storageProvider.GetTriggerById(job1.Id, trigger1.Id);

            Assert.IsFalse(triggerFromDb.IsActive);
        }

        [TestMethod]
        public void GivenDisabledTrigger_WhenEnabling_TriggerIsEnabled()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = false };

            _storageProvider.AddTrigger(job1.Id, trigger1);
            _storageProvider.EnableTrigger(job1.Id, trigger1.Id);

            var triggerFromDb = _storageProvider.GetTriggerById(job1.Id, trigger1.Id);
            
            Assert.IsTrue(triggerFromDb.IsActive);
        }

        [TestMethod]
        public void GivenJobRuns_WhenQueryingForLastJobRunByTrigger_LastJobRunIsReturned()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var now = DateTime.UtcNow;

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = now, ActualStartDateTimeUtc = now, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = now.AddMinutes(1), ActualStartDateTimeUtc = now.AddMinutes(1), State = JobRunStates.Completed };
            var jobRun3 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = now.AddMinutes(2), ActualStartDateTimeUtc = now.AddMinutes(2), State = JobRunStates.Completed };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);
            _storageProvider.AddJobRun(jobRun3);

            var lastJobRun = _storageProvider.GetLastJobRunByTriggerId(job1.Id, trigger1.Id, now.AddSeconds(30));

            Assert.AreEqual(jobRun1.Id, lastJobRun.Id);
        }

        [TestMethod]
        public void GivenJobRuns_WhenQueryingForNextJobRunByTrigger_NextJobRunIsReturned()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var now = DateTime.UtcNow;

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = now, ActualStartDateTimeUtc = now, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = now.AddMinutes(1), ActualStartDateTimeUtc = now.AddMinutes(1), State = JobRunStates.Completed };
            var jobRun3 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = now.AddMinutes(2), State = JobRunStates.Scheduled };

            _storageProvider.AddJobRun(jobRun1);
            _storageProvider.AddJobRun(jobRun2);
            _storageProvider.AddJobRun(jobRun3);

            var lastJobRun = _storageProvider.GetNextJobRunByTriggerId(job1.Id, trigger1.Id, now.AddMinutes(1));
            Assert.AreEqual(jobRun3.Id, lastJobRun.Id);
        }

        [TestMethod]
        public void GivenJobRun_WhenUpadingProgress_ProgressIsUpdated()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", CreatedDateTimeUtc = DateTime.UtcNow };

            _storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            _storageProvider.AddTrigger(job1.Id, trigger1);

            var now = DateTime.UtcNow;

            var jobRun1 = new JobRun { JobId = job1.Id, TriggerId = trigger1.Id, PlannedStartDateTimeUtc = now, ActualStartDateTimeUtc = now, State = JobRunStates.Completed };
         
            _storageProvider.AddJobRun(jobRun1);
           
            _storageProvider.UpdateProgress(jobRun1.Id, 50);

            var jobRun2 = _storageProvider.GetJobRunById(jobRun1.Id);

            Assert.AreEqual(50, jobRun2.Progress);
        }
    }
}
