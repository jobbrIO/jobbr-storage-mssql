namespace Jobbr.Storage.MsSql
{
    public class JobbrMsSqlConfiguration
    {
        /// <summary>
        /// ADO.NET Compatible Database connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Change the schema if you wish, defaults to "Jobbr"
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// We make sure that all upgrades are non destructive, don't worry!
        /// Before data deletion we would either ask you to backup data or preserve the old data in a backup schema.
        /// </summary>
        public bool AutoUpgrade { get; set; } = true;

        /// <summary>
        /// Set this value to true if you would like to automatically generate the database specified in the connection string if not existing.
        /// </summary>
        public bool AutoCreateDatabase { get; set; } = false;
    }
}