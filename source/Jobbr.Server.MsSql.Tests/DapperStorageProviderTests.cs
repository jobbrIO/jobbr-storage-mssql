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

        [TestInitialize]
        public void Initialize()
        {
            var localDb = new LocalDb();
            _sqlConnection = localDb.OpenConnection();
            _sqlConnection.Open();

            var sqlStatements = SqlHelper.SplitSqlStatements(File.ReadAllText("CreateSchemaAndTables.sql")).ToList();

            foreach (var statement in sqlStatements)
            {
                var command = new SqlCommand(statement, _sqlConnection);
                command.ExecuteNonQuery();
            }

            _storageProvider = new DapperStorageProvider(new JobbrMsSqlConfiguration
            {
                ConnectionString = localDb.ConnectionStringName,
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
    }
}
