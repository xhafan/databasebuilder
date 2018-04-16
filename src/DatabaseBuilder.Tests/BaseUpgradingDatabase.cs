using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper;
using Npgsql;
using NUnit.Framework;

namespace DatabaseBuilder.Tests
{
    public abstract class BaseUpgradingDatabase
    {
        protected ConnectionStringSettings _connectionStringSettings;
        protected string _connectionStringProviderName;
        protected string _assemblyLocation;
        protected string _newChangeScriptName;
        protected string _folderWithSqlFiles;

        [SetUp]
        public void TestFixtureSetUp()
        {
            _connectionStringSettings = ConfigurationManager.ConnectionStrings["DatabaseBuilderTestsConnection"];
            _connectionStringProviderName = _connectionStringSettings.ProviderName;

            _assemblyLocation = _GetAssemblyLocation();
            _folderWithSqlFiles = $"{_assemblyLocation}\\TestDatabase\\{_connectionStringProviderName}";
            var changeScriptsFolder = $"{_folderWithSqlFiles}\\ChangeScripts";
            _newChangeScriptName = $"{changeScriptsFolder}\\1.0.0.3.sql";

            DeleteNewChangeScriptIfExits();
        }

        protected void DeleteNewChangeScriptIfExits()
        {
            File.Delete(_newChangeScriptName);
        }

        protected void CreateNewChangeScript()
        {
            File.WriteAllText(_newChangeScriptName, @"
create table AnotherDataTable (
   Id INT not null,
   [Text] NVARCHAR(MAX) null,
   primary key (Id)
)

insert into AnotherDataTable (Id, [Text]) VALUES (2, 'some other text')
");
        }

        protected void DropDatabaseObjectsToMakeDatabaseEmpty()
        {
            try { ExecuteSql("drop table Version"); } catch (SqlException) {}
            try { ExecuteSql("drop table DataTable"); } catch (SqlException) {}
            try { ExecuteSql("drop table AnotherDataTable"); } catch (SqlException) {}
            try { ExecuteSql("drop view DataTableDto"); } catch (SqlException) {}
        }

        protected void ExecuteWithinTransaction(Action<IDbConnection, IDbTransaction> actionToExecute)
        {
            using (var connection = _GetDbConnection(_connectionStringSettings))
            {
                connection.Open();
                using (var tx = connection.BeginTransaction())
                {
                    try
                    {
                        actionToExecute(connection, tx);

                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        protected IEnumerable<T> ExecuteSqlQuery<T>(string sql)
        {
            using (var connection = _GetDbConnection(_connectionStringSettings))
            {
                connection.Open();
                return connection.Query<T>(sql);
            }
        }

        protected void ExecuteSql(string sql)
        {
            using (var connection = _GetDbConnection(_connectionStringSettings))
            {
                connection.Open();
                connection.Execute(sql);
            }
        }

        protected DatabaseVersion GetCurrentDatabaseVersion()
        {
            return ExecuteSqlQuery<DatabaseVersion>("select * from Version").Single();
        }


        private IDbConnection _GetDbConnection(ConnectionStringSettings connectionStringSettings)
        {
            var connectionString = connectionStringSettings.ConnectionString;

            switch (_connectionStringProviderName)
            {
                case string x when x.Contains("sqlite"):
                    return new SQLiteConnection(connectionString);
                case string x when x.Contains("sqlserver"):
                    return new SqlConnection(connectionString);
                case string x when x.Contains("postgresql"):
                    return new NpgsqlConnection(connectionString);
                default:
                    throw new Exception("Unsupported database provider");
            }
        }

        private string _GetAssemblyLocation()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var indexOfLastBackslash = assemblyLocation.LastIndexOf("\\", StringComparison.Ordinal);
            return assemblyLocation.Substring(0, indexOfLastBackslash);
        }
    }
}