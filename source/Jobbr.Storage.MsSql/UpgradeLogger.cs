using DbUp.Engine.Output;
using Jobbr.Server.MsSql.Logging;

namespace Jobbr.Storage.MsSql
{
    internal class UpgradeLogger : IUpgradeLog
    {
        private static readonly ILog Logger = LogProvider.For<MsSqlStorageProvider>();

        public void WriteInformation(string format, params object[] args)
        {
            Logger.InfoFormat(format, args);
        }

        public void WriteError(string format, params object[] args)
        {
            Logger.ErrorFormat(format, args);
        }

        public void WriteWarning(string format, params object[] args)
        {
            Logger.WarnFormat(format, args);
        }
    }
}