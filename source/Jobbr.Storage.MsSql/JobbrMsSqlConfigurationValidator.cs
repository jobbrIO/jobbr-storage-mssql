using System;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server.MsSql.Logging;

namespace Jobbr.Storage.MsSql
{
    internal class JobbrMsSqlConfigurationValidator : IConfigurationValidator
    {
        private static readonly ILog Logger = LogProvider.For<MsSqlStorageProvider>();

        public Type ConfigurationType { get; set; } = typeof(JobbrMsSqlConfiguration);

        public bool Validate(object configuration)
        {
            if (!(configuration is JobbrMsSqlConfiguration config))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.ConnectionString))
            {
                Logger.Error("No connection string provided.");
                return false;
            }

            return true;
        }
    }
}