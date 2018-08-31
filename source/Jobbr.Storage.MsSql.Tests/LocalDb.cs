using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;

namespace Jobbr.Storage.MsSql.Tests
{
    public class LocalDb : IDisposable
    {
        public static string DatabaseDirectory = "Data";

        public string ConnectionStringName { get; private set; }
        public string DatabaseName { get; private set; }
        public string OutputFolder { get; private set; }
        public string DatabaseMdfPath { get; private set; }
        public string DatabaseLogPath { get; private set; }

        static LocalDb()
        {
            try
            {
                Directory.Delete(GetOutputFolder(), true);
            }
            catch
            {
                // ignore
            }
        }

        public LocalDb(string databaseName = null)
        {
            DatabaseName = string.IsNullOrWhiteSpace(databaseName) ? Guid.NewGuid().ToString("N") : databaseName;
            CreateDatabase();
        }

        public SqlConnection CreateSqlConnection()
        {
            return new SqlConnection(ConnectionStringName);
        }

        private void CreateDatabase()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            //Console.WriteLine("Creating database with name " + DatabaseName);
            //OutputFolder = GetOutputFolder();
            //var mdfFilename = $"{DatabaseName}.mdf";
            //DatabaseMdfPath = Path.Combine(OutputFolder, mdfFilename);
            //DatabaseLogPath = Path.Combine(OutputFolder, $"{DatabaseName}_log.ldf");

            //if (!Directory.Exists(OutputFolder))
            //{
            //    Directory.CreateDirectory(OutputFolder);
            //}
            //else
            //{
            //    DeleteDatabaseFiles();
            //}

            //const string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=False;TrustServerCertificate=True";
            const string connectionString = "Data Source=localhost\\sqlexpress;Initial Catalog=JobbrTest;Integrated Security=True";

            //using (var connection = new SqlConnection(connectionString))
            //{
            //    connection.Open();
            //    var cmd = connection.CreateCommand();
            //    DetachDatabase();
            //    cmd.CommandText = $"CREATE DATABASE [{DatabaseName}] ON (NAME = N'{DatabaseName}', FILENAME = '{DatabaseMdfPath}')";
            //    cmd.ExecuteNonQuery();
            //}

            //ConnectionStringName = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDBFileName={DatabaseMdfPath};Initial Catalog={DatabaseName};Integrated Security=True;";
            ConnectionStringName = connectionString;
        }

        private static string GetOutputFolder()
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DatabaseDirectory);
        }

        private void DetachDatabase()
        {
            try
            {
                const string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True";

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = $"DROP DATABASE {DatabaseName}";
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                DeleteDatabaseFiles();
            }
        }

        public void DeleteDatabaseFiles()
        {
            if (File.Exists(DatabaseMdfPath)) File.Delete(DatabaseMdfPath);
            if (File.Exists(DatabaseLogPath)) File.Delete(DatabaseLogPath);
        }

        public void Dispose()
        {
            DetachDatabase();
        }
    }
}