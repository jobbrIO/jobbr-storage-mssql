using System.IO;
using System.Linq;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server;
using Jobbr.Server.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jobbr.Storage.MsSql.Tests
{
    [TestClass]
    public partial class ServerRegistrationTests
    {
        [TestMethod]
        public void RegisteredAsComponent_JobbrIsStarted_ProviderHasCorrectType()
        {
            var builder = new JobbrBuilder();
            builder.Register<IJobbrComponent>(typeof(ExposeStorageProvider));

            builder.AddMsSqlStorage(config =>
            {
                config.ConnectionString = @"Server=.\INSTANCENAME;Integrated Security=true;InitialCatalog=NotUsed;";
                config.Schema = "Jobbr";
            });

            builder.Create();

            Assert.AreEqual(typeof(MsSqlStorageProvider), ExposeStorageProvider.Instance.JobStorageProvider.GetType());
        }

        [TestMethod]
        public void RegisteredAsComponent_WithBasicConfiguration_DoesStart()
        {
            var localDb = new LocalDb();
            var sqlConnection = localDb.CreateSqlConnection();

            var builder = new JobbrBuilder();

            builder.AddMsSqlStorage(config =>
            {
                config.ConnectionString = sqlConnection.ConnectionString;
                config.Schema = "Jobbr";
            });

            using (var server = builder.Create())
            {
                server.Start();

                Assert.AreEqual(JobbrState.Running, server.State, "Server should be possible to start with default configuration");
            }
        }
    }
}