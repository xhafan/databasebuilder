﻿using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DatabaseBuilder
{
    /// <summary>
    /// Builds a versioned database from SQL script files in a directory structure.
    /// </summary>
    public class BuilderOfDatabase
    {
        private readonly Func<IDbConnection> _createConnectionFunc;
        private readonly string _versionTableName;
        private readonly string _changeScriptsDirectoryName;
        private readonly string _reRunnableScriptsDirectoryName;
        private readonly string _sqlScriptFileExtension;
        private readonly string _sqlScriptFileSearchPattern;
        private readonly Action<string> _logAction;

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="createConnectionFunc">A function to create a new database connection</param>
        /// <param name="versionTableName">A default table name for a table with the version info</param>
        /// <param name="changeScriptsDirectoryName">A name of the directory with change scripts</param>
        /// <param name="reRunnableScriptsDirectoryName">A name of the directory with re-runnable scripts</param>
        /// <param name="sqlScriptFileExtension">SQL script file extension</param>
        /// <param name="logAction">A log action. If not set it logs to a console.</param>
        public BuilderOfDatabase(
            Func<IDbConnection> createConnectionFunc,
            string versionTableName = "Version", 
            string changeScriptsDirectoryName = "ChangeScripts",
            string reRunnableScriptsDirectoryName = "ReRunnableScripts",
            string sqlScriptFileExtension = ".sql",
            Action<string>? logAction = null
            )
        {
            _createConnectionFunc = createConnectionFunc;
            _logAction = logAction ?? Console.WriteLine;
            _versionTableName = versionTableName;
            _changeScriptsDirectoryName = changeScriptsDirectoryName;
            _reRunnableScriptsDirectoryName = reRunnableScriptsDirectoryName;
            _sqlScriptFileExtension = sqlScriptFileExtension;
            _sqlScriptFileSearchPattern = $"*{_sqlScriptFileExtension}";
        }

        /// <summary>
        /// Builds the database.
        /// </summary>
        /// <param name="scriptsDirectoryPath">A path to a directory with the SQL scripts</param>
        public void BuildDatabase(string scriptsDirectoryPath)
        {
            for (var attemptNumber = 1; attemptNumber <= 2; attemptNumber++)
            {
                try
                {
                    var currentDatabaseVersion = _GetDatabaseVersionInNewTransaction();
                    _logAction($"Attempt {attemptNumber}: Current database version: {currentDatabaseVersion?.ToString() ?? "none"}");

                    _ExecuteWithinTransaction((connection, transaction) =>
                    {
                        _ApplyChangeScripts(currentDatabaseVersion, Path.Combine(scriptsDirectoryPath, _changeScriptsDirectoryName), connection, transaction);

                        var reRunnableScriptsDirectoryPath = Path.Combine(scriptsDirectoryPath, _reRunnableScriptsDirectoryName);
                        if (!Directory.Exists(reRunnableScriptsDirectoryPath)) return;
                        _ApplyReRunnableScripts(reRunnableScriptsDirectoryPath, connection, transaction);
                    });

                    break;
                }
                catch (CannotUpdateVersionException e)
                {
                    _logAction(e.Message);
                    if (attemptNumber == 2)
                    {
                        throw;
                    }
                }
            }
        }

        private void _ApplyChangeScripts(
            DatabaseVersion? currentDatabaseVersion, 
            string scriptsDirectoryPath,
            IDbConnection dbConnection, 
            IDbTransaction transaction
            )
        {
            var changeScriptSqlFiles = Directory.GetFiles(scriptsDirectoryPath, _sqlScriptFileSearchPattern);
            var orderedChangeScriptSqlFiles = changeScriptSqlFiles
                .OrderBy(changeScriptFileFullName =>
                {
                    var version = _GetChangeScriptVersionFromFullFileName(changeScriptFileFullName);
                    return new DatabaseVersion(version);
                })
                .ToList();

            var changeScriptSqlFilesGreaterThanCurrentDatabaseVersion = orderedChangeScriptSqlFiles.Where(
                changeScriptFileFullName =>
                {
                    var version = _GetChangeScriptVersionFromFullFileName(changeScriptFileFullName);
                    return currentDatabaseVersion == null || new DatabaseVersion(version).CompareTo(currentDatabaseVersion) > 0;
                }).ToList();

            if (!changeScriptSqlFilesGreaterThanCurrentDatabaseVersion.Any()) return;

            _logAction("Change scripts applied:");
            foreach (var changeScriptSqlFile in changeScriptSqlFilesGreaterThanCurrentDatabaseVersion)
            {
                if (currentDatabaseVersion != null)
                {
                    currentDatabaseVersion = _UpdateDatabaseVersion(
                        dbConnection,
                        transaction,
                        changeScriptSqlFile,
                        currentDatabaseVersion
                    );
                }

                _logAction(_GetChangeScriptVersionFromFullFileName(changeScriptSqlFile));
                _ApplyOneSqlScript(changeScriptSqlFile, dbConnection, transaction);

                currentDatabaseVersion ??= _GetDatabaseVersion(dbConnection, transaction);
            }

            _logAction($"Database version updated to {currentDatabaseVersion}");
        }

        private string _GetChangeScriptVersionFromFullFileName(string changeScriptFileFullName)
        {
            var indexOfLastBackslash = changeScriptFileFullName.LastIndexOf(Path.DirectorySeparatorChar);
            var changeScriptFileName = changeScriptFileFullName.Substring(indexOfLastBackslash + 1);
            return changeScriptFileName.Substring(0, changeScriptFileName.Length - _sqlScriptFileExtension.Length);
        }

        private void _ExecuteWithinTransaction(Action<IDbConnection, IDbTransaction> actionToExecute)
        {
            using var connection = _createConnectionFunc();
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                actionToExecute(connection, tx);

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { /* ignored */ }
                throw;
            }
        }

        private DatabaseVersion? _GetDatabaseVersionInNewTransaction()
        {
            DatabaseVersion? databaseVersion = null;

            _ExecuteWithinTransaction((connection, transaction) =>
            {
                databaseVersion = _GetDatabaseVersion(connection, transaction);
            });
            return databaseVersion;
        }

        private DatabaseVersion? _GetDatabaseVersion(IDbConnection connection, IDbTransaction transaction)
        {
            DatabaseVersion? databaseVersion = null;

            using var command = connection.CreateCommand();
            command.CommandText = $"""
                                   select "Major", "Minor", "Revision", "ScriptNumber" from "{_versionTableName}"
                                   """;
            command.Transaction = transaction;

            try
            {
                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    throw new CannotReadDatabaseVersionException(
                        $"The version table ({_versionTableName}) exists but the row with the version info is missing?");
                }

                var major = reader.GetInt32(0);
                var minor = reader.GetInt32(1);
                var revision = reader.GetInt32(2);
                var scriptNumber = reader.GetInt32(3);
                databaseVersion = new DatabaseVersion(major, minor, revision, scriptNumber);
            }
            catch (CannotReadDatabaseVersionException)
            {
                throw;
            }
            catch { /* ignored */ }

            return databaseVersion;
        }
        
        private DatabaseVersion _UpdateDatabaseVersion(
            IDbConnection dbConnection, 
            IDbTransaction transaction, 
            string changeScriptSqlFile,
            DatabaseVersion currentDatabaseVersion
            )
        {
            var changeScriptVersion = _GetChangeScriptVersionFromFullFileName(changeScriptSqlFile);
            var changeScriptDatabaseVersion = new DatabaseVersion(changeScriptVersion);

            try
            {
                using var command = dbConnection.CreateCommand();
                command.CommandText = $"""
                                       update "{_versionTableName}" set     
                                           "Major" = {changeScriptDatabaseVersion.Major}
                                           , "Minor" = {changeScriptDatabaseVersion.Minor}
                                           , "Revision" = {changeScriptDatabaseVersion.Revision}
                                           , "ScriptNumber" = {changeScriptDatabaseVersion.ScriptNumber} 
                                       where "Major" = {currentDatabaseVersion.Major} 
                                       and "Minor" = {currentDatabaseVersion.Minor} 
                                       and "Revision" = {currentDatabaseVersion.Revision} 
                                       and "ScriptNumber" = {currentDatabaseVersion.ScriptNumber} 
                                       """
                    ;
                command.Transaction = transaction;
                var numberOfRowsAffected = command.ExecuteNonQuery();
                if (numberOfRowsAffected != 1)
                {
                    throw new CannotUpdateVersionException(
                        "Database version has been changed in the meantime, possibly by another process concurrently?");
                }
            }
            catch (CannotUpdateVersionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot update database version in table {_versionTableName}", ex);
            }

            return changeScriptDatabaseVersion;
        }

        private void _ApplyReRunnableScripts(string scriptsDirectoryPath, IDbConnection dbConnection, IDbTransaction transaction)
        {
            var sqlScriptFilesOrderedByName = Directory.GetFiles(scriptsDirectoryPath, _sqlScriptFileSearchPattern)
                .OrderBy(x => x)
                .ToList();

            foreach (var sqlScriptFile in sqlScriptFilesOrderedByName)
            {
                _ApplyOneSqlScript(sqlScriptFile, dbConnection, transaction);
            }

            var directoriesOrderedByName = Directory.GetDirectories(scriptsDirectoryPath)
                .OrderBy(x => x)
                .ToList();

            foreach (var directory in directoriesOrderedByName)
            {
                _ApplyReRunnableScripts(directory, dbConnection, transaction);
            }
        }

        private void _ApplyOneSqlScript(string sqlScriptFile, IDbConnection dbConnection, IDbTransaction transaction)
        {
            var sqlScript = File.ReadAllText(sqlScriptFile);
            var sqlBatches = Regex.Split(sqlScript, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (var sqlBatch in sqlBatches)
            {
                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = sqlBatch;
                    command.Transaction = transaction;
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error executing script {sqlScriptFile}, sql batch:\n\n{sqlBatch}", ex);
                    }
                }
            }
        }
    }
}
