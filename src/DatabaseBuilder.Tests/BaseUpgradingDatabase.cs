using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper;
using Npgsql;
using NUnit.Framework;

#if !NETCOREAPP2_0
using System.Configuration;
#endif  

#if NETCOREAPP2_0
using Microsoft.Extensions.Configuration;
#endif

namespace DatabaseBuilder.Tests
{
    public abstract class BaseUpgradingDatabase
    {
        private string _connectionString;
        private string _dbProviderName;
        private string _assemblyLocation;
        private string _newChangeScriptName;

        protected string FolderWithSqlFiles;

        [SetUp]
        public void TestFixtureSetUp()
        {
            _LoadDbProviderAndConnectionString();

            _assemblyLocation = _GetAssemblyLocation();
            FolderWithSqlFiles = $"{_assemblyLocation}\\TestDatabase\\{_dbProviderName}";
            var changeScriptsFolder = $"{FolderWithSqlFiles}\\ChangeScripts";
            _newChangeScriptName = $"{changeScriptsFolder}\\1.0.0.10.sql";

            DeleteNewChangeScriptIfExits();
        }

        private void _LoadDbProviderAndConnectionString()
        {
#if NETCOREAPP2_0
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            var configuration = builder.Build();
            _dbProviderName = configuration["providerName"];
            _connectionString = configuration.GetConnectionString("DatabaseBuilderTestsConnection");
#else
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["DatabaseBuilderTestsConnection"];
            _dbProviderName = connectionStringSettings.ProviderName;
            _connectionString = connectionStringSettings.ConnectionString;
#endif
        }

        protected void DeleteNewChangeScriptIfExits()
        {
            File.Delete(_newChangeScriptName);
        }

        protected void CreateNewChangeScript()
        {
            File.WriteAllText(_newChangeScriptName, @"
alter table ""DataTable"" add Text2 Text null;
go
update ""DataTable"" SET Text2 = 'some other text' where id = 1
");
        }

        protected void DropDatabaseObjectsToMakeDatabaseEmpty()
        {
            try { ExecuteSql("drop table \"Version\""); } catch {}
            try { ExecuteSql("drop view \"DataTableDto\""); } catch { }
            try { ExecuteSql("drop table \"DataTable\""); } catch {}
        }

        protected IEnumerable<T> ExecuteSqlQuery<T>(string sql)
        {
            using (var connection = GetDbConnection())
            {
                connection.Open();
                return connection.Query<T>(sql);
            }
        }

        protected void ExecuteSql(string sql)
        {
            using (var connection = GetDbConnection())
            {
                connection.Open();
                connection.Execute(sql);
            }
        }

        protected DatabaseVersion GetCurrentDatabaseVersion()
        {
            return ExecuteSqlQuery<DatabaseVersion>("select * from \"Version\"").Single();
        }


        protected IDbConnection GetDbConnection()
        {
            switch (_dbProviderName)
            {
                case string x when x.Contains("sqlite"):
                    return new SQLiteConnection(_connectionString);
                case string x when x.Contains("sqlserver"):
                    return new SqlConnection(_connectionString);
                case string x when x.Contains("postgresql"):
                    return new NpgsqlConnection(_connectionString);
                default:
                    throw new Exception("Unsupported database provider");
            }
        }

        private string _GetAssemblyLocation()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var indexOfLastBackslash = assemblyLocation.LastIndexOf(Path.DirectorySeparatorChar);
            return assemblyLocation.Substring(0, indexOfLastBackslash);
        }
    }
}