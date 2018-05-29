using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DatabaseBuilder
{
    public class DatabaseBuilder
    {
        private readonly Func<IDbConnection> _getConnectionFunc;
        private readonly string _versionTableName;
        private readonly string _changeScriptsFolderName;
        private readonly string _otherScriptsFolderName;
        private readonly string _sqlScriptFileExtension;
        private readonly string _sqlScriptFileSearchPattern;

        public DatabaseBuilder(
            Func<IDbConnection> getConnectionFunc,
            string versionTableName = "Version", 
            string changeScriptsFolderName = "ChangeScripts",
            string otherScriptsFolderName = "OtherScripts",
            string sqlScriptFileExtension = ".sql"
            )
        {
            _getConnectionFunc = getConnectionFunc;
            _versionTableName = versionTableName;
            _changeScriptsFolderName = changeScriptsFolderName;
            _otherScriptsFolderName = otherScriptsFolderName;
            _sqlScriptFileExtension = sqlScriptFileExtension;
            _sqlScriptFileSearchPattern = $"*{_sqlScriptFileExtension}";
        }

        public void UpgradeDatabase(string folderWithSqlFiles)
        {
            var currentDatabaseVersion = _GetDatabaseVersion();

            _ExecuteWithinTransaction((connection, transaction) =>
            {
                _ApplyChangeScripts(currentDatabaseVersion, Path.Combine(folderWithSqlFiles, _changeScriptsFolderName), connection, transaction);
                _ApplyOtherScripts(Path.Combine(folderWithSqlFiles, _otherScriptsFolderName), connection, transaction);
            });
        }

        private void _ApplyChangeScripts(
            DatabaseVersion currentDatabaseVersion, 
            string folderWithSqlFiles,
            IDbConnection dbConnection, 
            IDbTransaction transaction
            )
        {
            var changeScriptSqlFiles = Directory.GetFiles(folderWithSqlFiles, _sqlScriptFileSearchPattern);
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
                    return new DatabaseVersion(version).CompareTo(currentDatabaseVersion) > 0;
                }).ToList();

            if (!changeScriptSqlFilesGreaterThanCurrentDatabaseVersion.Any()) return;

            foreach (var changeScriptSqlFile in changeScriptSqlFilesGreaterThanCurrentDatabaseVersion)
            {
                _ApplyOneSqlScript(changeScriptSqlFile, dbConnection, transaction);
            }

            _UpdateDatabaseVersion(dbConnection, transaction, changeScriptSqlFilesGreaterThanCurrentDatabaseVersion.Last());
        }

        private string _GetChangeScriptVersionFromFullFileName(string changeScriptFileFullName)
        {
            var indexOfLastBackslash = changeScriptFileFullName.LastIndexOf(Path.DirectorySeparatorChar);
            var changeScriptFileName = changeScriptFileFullName.Substring(indexOfLastBackslash + 1);
            return changeScriptFileName.Substring(0, changeScriptFileName.Length - _sqlScriptFileExtension.Length);
        }

        private void _ExecuteWithinTransaction(Action<IDbConnection, IDbTransaction> actionToExecute)
        {
            using (var connection = _getConnectionFunc())
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

        private DatabaseVersion _GetDatabaseVersion()
        {
            DatabaseVersion databaseVersion = new DatabaseVersion(0, 0, 0, 0);

            _ExecuteWithinTransaction((connection, transaction) =>
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $"select Major, Minor, Revision, ScriptNumber from \"{_versionTableName}\"";
                    command.Transaction = transaction;

                    try
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read()) throw new CannotReadDatabaseVersionException();
                            var major = reader.GetInt32(0);
                            var minor = reader.GetInt32(1);
                            var revision = reader.GetInt32(2);
                            var scriptNumber = reader.GetInt32(3);
                            databaseVersion = new DatabaseVersion(major, minor, revision, scriptNumber);
                        }
                    }
                    catch (CannotReadDatabaseVersionException)
                    {
                        throw;
                    }
                    catch {}
                }
            });
            return databaseVersion;
        }

        private void _UpdateDatabaseVersion(IDbConnection dbConnection, IDbTransaction transaction, string lastChangeScriptSqlFile)
        {
            var lastChangeScriptVersion = _GetChangeScriptVersionFromFullFileName(lastChangeScriptSqlFile);
            var databaseVersionOfLastChangeScript = new DatabaseVersion(lastChangeScriptVersion);

            try
            {
                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = $"update \"{_versionTableName}\" set " +
                                          $"    Major = {databaseVersionOfLastChangeScript.Major}, " +
                                          $"    Minor = {databaseVersionOfLastChangeScript.Minor}, " +
                                          $"    Revision = {databaseVersionOfLastChangeScript.Revision}, " +
                                          $"    ScriptNumber = {databaseVersionOfLastChangeScript.ScriptNumber}";
                    command.Transaction = transaction;
                    command.ExecuteNonQuery();    
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot update database version in table {_versionTableName}", ex);
            }
        }

        private void _ApplyOtherScripts(string folderWithSqlFiles, IDbConnection dbConnection, IDbTransaction transaction)
        {
            var sqlScriptFilesOrderedByName = Directory.GetFiles(folderWithSqlFiles, _sqlScriptFileSearchPattern)
                .OrderBy(x => x)
                .ToList();

            foreach (var sqlScriptFile in sqlScriptFilesOrderedByName)
            {
                _ApplyOneSqlScript(sqlScriptFile, dbConnection, transaction);
            }

            var foldersOrderedByName = Directory.GetDirectories(folderWithSqlFiles)
                .OrderBy(x => x)
                .ToList();

            foreach (var folder in foldersOrderedByName)
            {
                _ApplyOtherScripts(folder, dbConnection, transaction);
            }
        }

        private void _ApplyOneSqlScript(string sqlScriptFile, IDbConnection dbConnection, IDbTransaction transaction)
        {
            var sqlScript = File.ReadAllText(sqlScriptFile);
            var sqlBatches = Regex.Split(sqlScript, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

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
