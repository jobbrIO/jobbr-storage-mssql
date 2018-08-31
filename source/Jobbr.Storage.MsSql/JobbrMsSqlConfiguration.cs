using System;
using ServiceStack.OrmLite;

namespace Jobbr.Storage.MsSql
{
    public class JobbrMsSqlConfiguration
    {
        public string ConnectionString { get; set; }

        [Obsolete("Schema property is not used anymore. Give jobbr its own database - no schemas needed")]
        public string Schema { get; set; }

        public IOrmLiteDialectProvider DialectProvider { get; set; }
    }
}