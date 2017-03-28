
using Jobbr.ComponentModel.JobStorage;
using Jobbr.ComponentModel.Registration;
// using Jobbr.Server.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Server.MsSql.Tests
{
    [TestClass]
    public class ServerRegistrationTests
    {
        [TestMethod]
        public void RegisteredAsComponent_JobbrIsStarted_ProviderHasCorrectType()
        {
            Assert.Inconclusive("Unable to test now, server needs to implement StorageModel >= rc11");
            //var builder = new JobbrBuilder();
            //builder.AddMsSqlStorage(config =>
            //{
            //    config.ConnectionString = @"Server=.\INSTANCENAME;Integrated Security=true;InitialCatalog=NotUsed;";
            //    config.Schema = "Jobbr";
            //});

            //builder.Create();

            //Assert.AreEqual(typeof(DapperStorageProvider), ExposeStorageProvider.Instance.jobStorageProvider.GetType());
        }

        [TestMethod]
        public void RegisteredAsComponent_WithBasicConfiguration_DoesStart()
        {
            Assert.Inconclusive("Unable to test now, server needs to implement StorageModel >= rc11");
            //var builder = new JobbrBuilder();
            //builder.AddMsSqlStorage(config =>
            //{
            //    config.ConnectionString = @"Server=.\INSTANCENAME;Integrated Security=true;InitialCatalog=NotUsed;";
            //    config.Schema = "Jobbr";
            //});

            //using (var server = builder.Create())
            //{
            //    server.Start();

            //    Assert.AreEqual(JobbrState.Running, server.State, "Server should be possible to start with default configuration");
            //}
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