using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using Jobbr.ComponentModel.JobStorage.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.SqlServer;
using Shouldly;

namespace Jobbr.Storage.MsSql.Tests
{
    [TestClass]
    public class MsSqlStorageProviderTests
    {
        private MsSqlStorageProvider storageProvider;

        private const string ConnectionString = "Data Source=localhost\\sqlexpress;Initial Catalog=JobbrTest;Integrated Security=True";

        [TestInitialize]
        public void SetupDatabaseInstance()
        {
            //OrmLiteConfig.BeforeExecFilter = dbCmd => File.AppendAllText("c:/temp/sql.txt", Environment.NewLine + dbCmd.GetDebugString() + Environment.NewLine);

            DropTablesIfExists();

            this.storageProvider = new MsSqlStorageProvider(new JobbrMsSqlConfiguration
            {
                ConnectionString = ConnectionString,
                DialectProvider = new SqlServer2017OrmLiteDialectProvider(),
                CreateTablesIfNotExists = true
            });
        }

        private static void DropTablesIfExists()
        {
            var factory = new OrmLiteConnectionFactory(ConnectionString, new SqlServer2017OrmLiteDialectProvider());
            var connection = factory.CreateDbConnection();
            connection.Open();

            if (connection.TableExists<Entities.JobRun>())
            {
                connection.DropTable<Entities.JobRun>();
            }

            if (connection.TableExists<Entities.Trigger>())
            {
                connection.DropTable<Entities.Trigger>();
            }

            if (connection.TableExists<Entities.Job>())
            {
                connection.DropTable<Entities.Job>();
            }

            connection.Close();
        }

        [TestMethod]
        public void Adding_Jobs()
        {
            var job1 = new Job
            {
                UniqueName = "testjob",
                Type = "Jobs.Test"
            };

            var job2 = new Job
            {
                UniqueName = "testjob2",
                Type = "Jobs.Test2"
            };

            storageProvider.AddJob(job1);
            storageProvider.AddJob(job2);

            job1.Id.ShouldNotBe(job2.Id);
        }

        [TestMethod]
        public void Get_Job_By_Id()
        {
            var job = new Job
            {
                UniqueName = "testjob",
                Type = "Jobs.Test"
            };

            storageProvider.AddJob(job);

            var job2 = storageProvider.GetJobById(job.Id);

            job.Id.ShouldBe(job2.Id);
            job2.UniqueName.ShouldBe("testjob");
            job2.Type.ShouldBe("Jobs.Test");
        }

        [TestMethod]
        public void UniqueName_Is_Unique()
        {
            var job1 = new Job
            {
                UniqueName = "i-am-unique",
            };

            var job2 = new Job
            {
                UniqueName = "i-am-unique",
            };

            storageProvider.AddJob(job1);

            Should.Throw<SqlException>(() => storageProvider.AddJob(job2), "SqlException should have been raised");
        }

        [TestMethod]
        public void Query_Job_By_Unique_Name()
        {
            var job = new Job
            {
                UniqueName = "testjob",
                Type = "Jobs.Test"
            };

            this.storageProvider.AddJob(job);

            var job2 = this.storageProvider.GetJobByUniqueName(job.UniqueName);

            job.Id.ShouldBe(job2.Id);
            job2.UniqueName.ShouldBe("testjob");
            job2.Type.ShouldBe("Jobs.Test");
        }

        [TestMethod]
        public void Query_Jobs_Paged()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };
            var job2 = new Job { UniqueName = "testjob2", Type = "Jobs.Test2" };
            var job3 = new Job { UniqueName = "testjob3", Type = "Jobs.Test3" };

            this.storageProvider.AddJob(job1);
            this.storageProvider.AddJob(job2);
            this.storageProvider.AddJob(job3);

            var jobs = this.storageProvider.GetJobs(1, 1);

            jobs.Items.Count.ShouldBe(1);
        }

        [TestMethod]
        public void Query_Jobs_Page_Two()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };
            var job2 = new Job { UniqueName = "testjob2", Type = "Jobs.Test2" };
            var job3 = new Job { UniqueName = "testjob3", Type = "Jobs.Test3" };

            this.storageProvider.AddJob(job1);
            this.storageProvider.AddJob(job2);
            this.storageProvider.AddJob(job3);

            var jobs = this.storageProvider.GetJobs(2, 1);

            Assert.AreEqual(1, jobs.Items.Count);
            Assert.AreEqual(job2.Id, jobs.Items[0].Id);

            jobs.Items.Count.ShouldBe(1);
            jobs.Items[0].Id.ShouldBe(job2.Id);
        }

        [TestMethod]
        public void Querying_Only_Active_Triggers()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };
            var job2 = new Job { UniqueName = "testjob2", Type = "Jobs.Test2" };

            this.storageProvider.AddJob(job1);
            this.storageProvider.AddJob(job2);

            var trigger1 = new InstantTrigger { IsActive = false };
            var trigger2 = new InstantTrigger { IsActive = true };
            var trigger3 = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger1);
            this.storageProvider.AddTrigger(job1.Id, trigger2);
            this.storageProvider.AddTrigger(job2.Id, trigger3);

            var activeTriggers = this.storageProvider.GetActiveTriggers();

            activeTriggers.Items.Count.ShouldBe(2);
        }

        [TestMethod]
        public void Querying_Triggers_Of_Job()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };
            var job2 = new Job { UniqueName = "testjob2", Type = "Jobs.Test2" };

            this.storageProvider.AddJob(job1);
            this.storageProvider.AddJob(job2);

            var trigger1 = new InstantTrigger();
            var trigger2 = new InstantTrigger();
            var trigger3 = new InstantTrigger();

            this.storageProvider.AddTrigger(job1.Id, trigger1);
            this.storageProvider.AddTrigger(job1.Id, trigger2);
            this.storageProvider.AddTrigger(job2.Id, trigger3);

            var triggersOfJob1 = this.storageProvider.GetTriggersByJobId(job1.Id);
            var triggersOfJob2 = this.storageProvider.GetTriggersByJobId(job2.Id);

            triggersOfJob1.Items.Count.ShouldBe(2);
            triggersOfJob2.Items.Count.ShouldBe(1);
        }

        [TestMethod]
        public void Get_JobRun_By_Id()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1", Parameters = "param", Title = "title" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true, DelayedMinutes = 1337, Parameters = "triggerparams", Comment = "comment", UserDisplayName = "chefkoch", UserId = "ck" };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { Job = job1, Trigger = trigger1, PlannedStartDateTimeUtc = DateTime.UtcNow, JobParameters = "param", InstanceParameters = "triggerparams" };

            this.storageProvider.AddJobRun(jobRun1);

            var jobRun2 = this.storageProvider.GetJobRunById(jobRun1.Id);

            jobRun1.Id.ShouldBe(jobRun2.Id);

            jobRun2.Trigger.ShouldBeOfType<InstantTrigger>();
            jobRun2.InstanceParameters.ShouldBe("triggerparams");
            jobRun2.JobParameters.ShouldBe("param");
        }

        [TestMethod]
        public void Query_JobRuns_Page_Two()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun
            {
                Job = new Job { Id = job1.Id },
                Trigger = new InstantTrigger { Id = trigger1.Id },
                PlannedStartDateTimeUtc = DateTime.UtcNow,
            };

            var jobRun2 = new JobRun
            {
                Job = new Job { Id = job1.Id },
                Trigger = new InstantTrigger { Id = trigger1.Id },
                PlannedStartDateTimeUtc = DateTime.UtcNow,
            };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);

            var jobRuns = this.storageProvider.GetJobRuns(1, 1);

            jobRuns.Items.Count.ShouldBe(1);

            jobRuns = this.storageProvider.GetJobRuns(1, 2);

            jobRuns.Items.Count.ShouldBe(2);
        }

        [TestMethod]
        public void Query_JobRuns_For_Specific_State()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Failed };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);
            this.storageProvider.AddJobRun(jobRun3);

            var jobRuns = this.storageProvider.GetJobRunsByState(JobRunStates.Failed);

            jobRuns.Items.Count.ShouldBe(1);
        }

        [TestMethod]
        public void Query_JobRuns_For_Specific_State_Paged()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Failed };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);
            this.storageProvider.AddJobRun(jobRun3);

            var jobRuns = this.storageProvider.GetJobRunsByState(JobRunStates.Completed, 1, 1);

            jobRuns.Items.Count.ShouldBe(1);

            jobRuns = this.storageProvider.GetJobRunsByState(JobRunStates.Completed, 1, 2);

            jobRuns.Items.Count.ShouldBe(2);
        }

        [TestMethod]
        public void Get_JobRuns_By_Trigger_Id()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Failed };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);
            this.storageProvider.AddJobRun(jobRun3);

            var jobRuns = this.storageProvider.GetJobRunsByTriggerId(job1.Id, trigger1.Id);

            jobRuns.Items.Count.ShouldBe(3);
        }

        [TestMethod]
        public void Get_JobRuns_By_Trigger_Id_Paged()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Failed };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);
            this.storageProvider.AddJobRun(jobRun3);

            var jobRuns = this.storageProvider.GetJobRunsByTriggerId(job1.Id, trigger1.Id, 1, 2);

            jobRuns.Items.Count.ShouldBe(2);
        }

        [TestMethod]
        public void Get_JobRuns_By_UserDisplayName()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true, UserDisplayName = "chefkoch" };
            var trigger2 = new InstantTrigger { IsActive = true, UserDisplayName = "foo" };

            this.storageProvider.AddTrigger(job1.Id, trigger1);
            this.storageProvider.AddTrigger(job1.Id, trigger2);

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger2.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);
            this.storageProvider.AddJobRun(jobRun3);

            var jobRuns = this.storageProvider.GetJobRunsByUserDisplayName("chefkoch");

            jobRuns.Items.Count.ShouldBe(2);
        }

        [TestMethod]
        public void Get_JobRuns_by_UserDisplayName()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true, UserDisplayName = "chefkoch" };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);
            this.storageProvider.AddJobRun(jobRun3);

            var jobRuns = this.storageProvider.GetJobRunsByUserDisplayName("chefkoch", 1, 2);

            jobRuns.Items.Count.ShouldBe(2);
        }

        [TestMethod]
        public void Get_JobRuns_By_UserId()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true, UserId = "ozu" };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);
            this.storageProvider.AddJobRun(jobRun3);

            var jobRuns = this.storageProvider.GetJobRunsByUserId("ozu");

            jobRuns.Items.Count.ShouldBe(3);
        }

        [TestMethod]
        public void Get_JobRuns_By_UserId_Paged()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true, UserId = "ozu" };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };
            var jobRun3 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow, State = JobRunStates.Completed };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);
            this.storageProvider.AddJobRun(jobRun3);

            var jobRuns = this.storageProvider.GetJobRunsByUserId("ozu", 1, 2);

            jobRuns.Items.Count.ShouldBe(2);
        }

        [TestMethod]
        public void Disable_Trigger()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            this.storageProvider.DisableTrigger(job1.Id, trigger1.Id);

            var triggerFromDb = this.storageProvider.GetTriggerById(job1.Id, trigger1.Id);

            triggerFromDb.IsActive.ShouldBeFalse();
        }

        [TestMethod]
        public void Enabling_Trigger()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = false };

            this.storageProvider.AddTrigger(job1.Id, trigger1);
            this.storageProvider.EnableTrigger(job1.Id, trigger1.Id);

            var triggerFromDb = this.storageProvider.GetTriggerById(job1.Id, trigger1.Id);

            triggerFromDb.IsActive.ShouldBeTrue();
        }

        [TestMethod]
        public void Get_Last_JobRun_By_TriggerId()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var now = DateTime.UtcNow;

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = now, ActualStartDateTimeUtc = now, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = now.AddMinutes(1), ActualStartDateTimeUtc = now.AddMinutes(1), State = JobRunStates.Completed };
            var jobRun3 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = now.AddMinutes(2), ActualStartDateTimeUtc = now.AddMinutes(2), State = JobRunStates.Completed };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);
            this.storageProvider.AddJobRun(jobRun3);

            var lastJobRun = this.storageProvider.GetLastJobRunByTriggerId(job1.Id, trigger1.Id, now.AddSeconds(30));

            lastJobRun.Id.ShouldBe(jobRun2.Id);
        }

        [TestMethod]
        public void Get_Next_JobRun_By_TriggerId()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var now = DateTime.UtcNow;

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = now, ActualStartDateTimeUtc = now, State = JobRunStates.Completed };
            var jobRun2 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = now.AddMinutes(1), ActualStartDateTimeUtc = now.AddMinutes(1), State = JobRunStates.Completed };
            var jobRun3 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = now.AddMinutes(2), State = JobRunStates.Scheduled };

            this.storageProvider.AddJobRun(jobRun1);
            this.storageProvider.AddJobRun(jobRun2);
            this.storageProvider.AddJobRun(jobRun3);

            var nextJobRun = this.storageProvider.GetNextJobRunByTriggerId(job1.Id, trigger1.Id, now.AddMinutes(1));

            nextJobRun.Id.ShouldBe(jobRun3.Id);
        }

        [TestMethod]
        public void Updating_Progress()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger1 = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger1);

            var now = DateTime.UtcNow;

            var jobRun1 = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger1.Id }, PlannedStartDateTimeUtc = now, ActualStartDateTimeUtc = now, State = JobRunStates.Completed };

            this.storageProvider.AddJobRun(jobRun1);

            this.storageProvider.UpdateProgress(jobRun1.Id, 50);

            var jobRun2 = this.storageProvider.GetJobRunById(jobRun1.Id);

            jobRun2.Progress.ShouldBe(50);
        }

        [TestMethod]
        public void Updating_Job()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            job1.UniqueName = "test-uniquename";
            job1.Title = "test-title";
            job1.Type = "test-type";
            job1.Parameters = "test-parameters";

            this.storageProvider.Update(job1);

            var job1Reloaded = this.storageProvider.GetJobById(job1.Id);

            job1Reloaded.UniqueName.ShouldBe("test-uniquename");
            job1Reloaded.Title.ShouldBe("test-title");
            job1Reloaded.Type.ShouldBe("test-type");
            job1Reloaded.Parameters.ShouldBe("test-parameters");
        }

        [TestMethod]
        public void Updating_InstantTrigger()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger = new InstantTrigger { IsActive = true };

            this.storageProvider.AddTrigger(job1.Id, trigger);

            var jobRun = new JobRun { Job = new Job { Id = job1.Id }, Trigger = new InstantTrigger { Id = trigger.Id }, PlannedStartDateTimeUtc = DateTime.UtcNow };

            this.storageProvider.AddJobRun(jobRun);

            var newPlannedStartDate = DateTime.UtcNow;
            var newActualStartDate = newPlannedStartDate.AddSeconds(1);
            var newEstimatedStartDate = newPlannedStartDate.AddMilliseconds(1);
            var newActualEndDate = newPlannedStartDate.AddMinutes(1);

            jobRun.JobParameters = "test-jobparameters";
            jobRun.InstanceParameters = "test-instanceparameters";
            jobRun.PlannedStartDateTimeUtc = newPlannedStartDate;
            jobRun.ActualStartDateTimeUtc = newActualStartDate;
            jobRun.EstimatedEndDateTimeUtc = newEstimatedStartDate;
            jobRun.ActualEndDateTimeUtc = newActualEndDate;

            this.storageProvider.Update(jobRun);

            var job1Reloaded = this.storageProvider.GetJobRunById(job1.Id);

            job1Reloaded.JobParameters.ShouldBe("test-jobparameters");
            job1Reloaded.InstanceParameters.ShouldBe("test-instanceparameters");
            job1Reloaded.PlannedStartDateTimeUtc.ShouldBe(newPlannedStartDate, TimeSpan.FromSeconds(1));
            job1Reloaded.ActualStartDateTimeUtc.GetValueOrDefault().ShouldBe(newActualStartDate, TimeSpan.FromSeconds(1));
            job1Reloaded.EstimatedEndDateTimeUtc.GetValueOrDefault().ShouldBe(newEstimatedStartDate, TimeSpan.FromSeconds(1));
            job1Reloaded.ActualEndDateTimeUtc.GetValueOrDefault().ShouldBe(newActualEndDate, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void Update_InstantTrigger()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger = new InstantTrigger();

            this.storageProvider.AddTrigger(job1.Id, trigger);

            var trigger2 = (InstantTrigger)this.storageProvider.GetTriggerById(job1.Id, trigger.Id);
            trigger2.Comment = "bla";
            trigger2.IsActive = true;
            trigger2.Parameters = "test-parameters";
            trigger2.UserId = "ozu";
            trigger2.DelayedMinutes = 5;

            this.storageProvider.Update(job1.Id, trigger2);

            var trigger2Reloaded = (InstantTrigger)this.storageProvider.GetTriggerById(job1.Id, trigger2.Id);

            trigger2Reloaded.Comment.ShouldBe("bla");
            trigger2Reloaded.IsActive.ShouldBeTrue();
            trigger2Reloaded.Parameters.ShouldBe("test-parameters");
            trigger2Reloaded.UserId.ShouldBe("ozu");
            trigger2Reloaded.DelayedMinutes.ShouldBe(5);
        }

        [TestMethod]
        public void Updating_ScheduledTrigger()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger = new ScheduledTrigger { StartDateTimeUtc = DateTime.UtcNow };

            this.storageProvider.AddTrigger(job1.Id, trigger);

            var trigger2 = (ScheduledTrigger)this.storageProvider.GetTriggerById(job1.Id, trigger.Id);

            var startDateTime = DateTime.UtcNow.AddHours(5);

            trigger2.Comment = "bla";
            trigger2.IsActive = true;
            trigger2.Parameters = "test-parameters";
            trigger2.UserId = "ozu";
            trigger2.StartDateTimeUtc = startDateTime;

            this.storageProvider.Update(job1.Id, trigger2);

            var trigger2Reloaded = (ScheduledTrigger)this.storageProvider.GetTriggerById(job1.Id, trigger2.Id);

            trigger2Reloaded.Comment.ShouldBe("bla");
            trigger2Reloaded.IsActive.ShouldBeTrue();
            trigger2Reloaded.Parameters.ShouldBe("test-parameters");
            trigger2Reloaded.UserId.ShouldBe("ozu");
            trigger2Reloaded.StartDateTimeUtc.ShouldBe(startDateTime, TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void Updating_RecurringTrigger()
        {
            var job1 = new Job { UniqueName = "testjob1", Type = "Jobs.Test1" };

            this.storageProvider.AddJob(job1);

            var trigger = new RecurringTrigger();

            this.storageProvider.AddTrigger(job1.Id, trigger);

            var trigger2 = (RecurringTrigger)this.storageProvider.GetTriggerById(job1.Id, trigger.Id);

            var startDateTime = DateTime.UtcNow.AddHours(5);
            var endDateTime = DateTime.UtcNow.AddHours(7);

            trigger2.Comment = "bla";
            trigger2.IsActive = true;
            trigger2.Parameters = "test-parameters";
            trigger2.UserId = "ozu";
            trigger2.StartDateTimeUtc = startDateTime;
            trigger2.Definition = "* * * * *";
            trigger2.EndDateTimeUtc = endDateTime;
            trigger2.NoParallelExecution = true;

            this.storageProvider.Update(job1.Id, trigger2);

            var trigger2Reloaded = (RecurringTrigger)this.storageProvider.GetTriggerById(job1.Id, trigger2.Id);

            trigger2Reloaded.Comment.ShouldBe("bla");
            trigger2Reloaded.IsActive.ShouldBeTrue();
            trigger2Reloaded.Parameters.ShouldBe("test-parameters");
            trigger2Reloaded.UserId.ShouldBe("ozu");
            trigger2Reloaded.StartDateTimeUtc.GetValueOrDefault().ShouldBe(startDateTime, TimeSpan.FromSeconds(1));
            trigger2Reloaded.EndDateTimeUtc.GetValueOrDefault().ShouldBe(endDateTime, TimeSpan.FromSeconds(1));
            trigger2Reloaded.Definition.ShouldBe("* * * * *");
            trigger2Reloaded.NoParallelExecution.ShouldBeTrue();
        }

        [TestMethod]
        public void GivenRunningDatabase_WhenCheckingAvailability_IsAvailable()
        {
            Assert.IsTrue(this.storageProvider.IsAvailable());
        }

        [TestMethod]
        public void Get_Jobs_Count()
        {
            var job = new Job
            {
                UniqueName = "testjob",
                Type = "Jobs.Test"
            };

            this.storageProvider.AddJob(job);

            var jobCount = this.storageProvider.GetJobsCount();

            jobCount.ShouldBe(1);
        }
    }
}
