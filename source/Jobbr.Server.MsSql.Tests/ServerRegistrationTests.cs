using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Jobbr.ComponentModel.JobStorage;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.MsSql.Tests
{
    [TestClass]
    public class ServerRegistrationTests
    {
        private LocalDb localDb;

        [TestInitialize]
        public void SetUp()
        {
            localDb = new LocalDb();
            var sqlConnection = localDb.OpenConnection();
            sqlConnection.Open();

            var sqlStatements = SqlHelper.SplitSqlStatements(File.ReadAllText("CreateSchemaAndTables.sql")).ToList();

            foreach (var statement in sqlStatements)
            {
                var command = new SqlCommand(statement, sqlConnection);
                command.ExecuteNonQuery();
            }
        }

        [TestMethod]
        public void RegisteredAsComponent_JobbrIsStarted_ProviderHasCorrectType()
        {
            var builder = new JobbrBuilder();
            builder.AddMsSqlStorage(config =>
            {
                config.ConnectionString = localDb.ConnectionStringName;
                config.Schema = "Jobbr";
            });

            builder.Create();

            Assert.AreEqual(typeof(DapperStorageProvider), ExposeStorageProvider.Instance.jobStorageProvider.GetType());
        }

        [TestMethod]
        public void RegisteredAsComponent_WithBasicConfiguration_DoesStart()
        {
            var builder = new JobbrBuilder();
            builder.AddMsSqlStorage(config =>
            {
                config.ConnectionString = localDb.ConnectionStringName;
                config.Schema = "Jobbr";
            });

            using (var server = builder.Create())
            {
                server.Start();

                Assert.AreEqual(JobbrState.Running, server.State, "Server should be possible to start with default configuration");
            }
        }

        public class ExposeStorageProvider : IJobbrComponent
        {
            internal readonly IJobStorageProvider jobStorageProvider;

            public static ExposeStorageProvider Instance;

            public ExposeStorageProvider(IJobStorageProvider jobStorageProvider)
            {
                this.jobStorageProvider = jobStorageProvider;
                Instance = this;
            }
            public void Dispose()
            {
            }

            public void Start()
            {
            }

            public void Stop()
            {
            }
        }
    }
}