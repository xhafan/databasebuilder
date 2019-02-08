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

#if !NETCOREAPP
using System.Configuration;
#endif  

#if NETCOREAPP
using Microsoft.Extensions.Configuration;
#endif

namespace DatabaseBuilder.Tests
{
    public abstract class BaseBuildingDatabase
    {
        private string _connectionString;
        private string _dbProviderName;
        private string _assemblyLocation;
        private string _newChangeScriptName;

        protected string SqlFilesDirectoryPath;

        [SetUp]
        public void TestFixtureSetUp()
        {
            _LoadDbProviderAndConnectionString();

            _assemblyLocation = _GetAssemblyLocation();
            SqlFilesDirectoryPath = $"{_assemblyLocation}\\TestDatabase\\{_dbProviderName}";
            var changeScriptsFolder = $"{SqlFilesDirectoryPath}\\ChangeScripts";
            _newChangeScriptName = $"{changeScriptsFolder}\\1.0.0.10.sql";

            DeleteNewChangeScriptIfExits();
        }

        private void _LoadDbProviderAndConnectionString()
        {
#if NETCOREAPP
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
alter table ""DataTable"" add ""Text2"" Text null;
go
update ""DataTable"" SET ""Text2"" = 'some other text' where ""Id"" = 1
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

        protected string GetCurrentDatabaseVersion()
        {
            using (var connection = GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"select \"Major\", \"Minor\", \"Revision\", \"ScriptNumber\" from \"Version\"";

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) throw new Exception("Cannot read database version");
                        var major = reader.GetInt32(0);
                        var minor = reader.GetInt32(1);
                        var revision = reader.GetInt32(2);
                        var scriptNumber = reader.GetInt32(3);
                        return $"{major}.{minor}.{revision}.{scriptNumber}";
                    }
                }
            }
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