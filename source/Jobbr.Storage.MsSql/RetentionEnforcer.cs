using System;
using System.Threading;

namespace Jobbr.Storage.MsSql
{
    public class RetentionEnforcer
    {
        private readonly Timer timer;

        public RetentionEnforcer(MsSqlStorageProvider storage, TimeSpan retention, TimeSpan interval)
        {
            this.timer = new Timer(state =>
            {
                var deadline = DateTimeOffset.UtcNow.Subtract(retention);

                storage.ApplyRetention(deadline);
            });

            this.timer.Change(TimeSpan.Zero, interval);
        }
    }
}
