using System;
using Jobbr.ComponentModel.Registration;
using Jobbr.Server.MsSql.Logging;

namespace Jobbr.Server.MsSql
{
    internal class JobbrMsSqlConfigurationValidator : IConfigurationValidator
    {
        private static readonly ILog Logger = LogProvider.For<MsSqlStorageProvider>();

        public Type ConfigurationType { get; set; } = typeof(JobbrMsSqlConfiguration);

        public bool Validate(object configuration)
        {
            var config = configuration as JobbrMsSqlConfiguration;

            if (config == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.Schema))
            {
                config.Schema = "Jobbr";
                Logger.Debug($"No schema provided. Using default schema '${config.Schema}'");
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