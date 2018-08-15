using ServiceStack.OrmLite;

namespace Jobbr.Storage.MsSql
{
    public class JobbrMsSqlConfiguration
    {
        public string ConnectionString { get; set; }

        public string Schema { get; set; }

        public IOrmLiteDialectProvider DialectProvider { get; set; }
    }
}