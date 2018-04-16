using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DatabaseBuilder
{
    public class DatabaseBuilder
    {
        private readonly string _versionTableName;
        private readonly string _changeScriptsFolderName;
        private readonly string _otherScriptsFolderName;
        private readonly string _sqlScriptFileExtension;
        private readonly string _sqlScriptFileSearchPattern;

        public DatabaseBuilder(
            string versionTableName = "Version", 
            string changeScriptsFolderName = "ChangeScripts",
            string otherScriptsFolderName = "OtherScripts",
            string sqlScriptFileExtension = ".sql"
            )
        {
            _versionTableName = versionTableName;
            _changeScriptsFolderName = changeScriptsFolderName;
            _otherScriptsFolderName = otherScriptsFolderName;
            _sqlScriptFileExtension = sqlScriptFileExtension;
            _sqlScriptFileSearchPattern = $"*{_sqlScriptFileExtension}";
        }

        public void UpgradeDatabase(string folderWithSqlFiles, IDbConnection dbConnection, IDbTransaction transaction)
        {
            _ApplyChangeScripts($"{folderWithSqlFiles}\\{_changeScriptsFolderName}", dbConnection, transaction);
            _ApplyOtherScripts($"{folderWithSqlFiles}\\{_otherScriptsFolderName}", dbConnection, transaction);

        }

        private void _ApplyChangeScripts(string folderWithSqlFiles, IDbConnection dbConnection, IDbTransaction transaction)
        {
            var currentDatabaseVersion = _GetDatabaseVersion(dbConnection, transaction);

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


            foreach (var changeScriptSqlFile in changeScriptSqlFilesGreaterThanCurrentDatabaseVersion)
            {
                _ApplyOneSqlScript(changeScriptSqlFile, dbConnection, transaction);
            }

            _UpdateDatabaseVersion(dbConnection, transaction, orderedChangeScriptSqlFiles);
        }

        private string _GetChangeScriptVersionFromFullFileName(string changeScriptFileFullName)
        {
            var indexOfLastBackslash = changeScriptFileFullName.LastIndexOf("\\", StringComparison.Ordinal);
            var changeScriptFileName = changeScriptFileFullName.Substring(indexOfLastBackslash + 1);
            return changeScriptFileName.Substring(0, changeScriptFileName.Length - _sqlScriptFileExtension.Length);
        }

        private DatabaseVersion _GetDatabaseVersion(IDbConnection dbConnection, IDbTransaction transaction)
        {
            try
            {
                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = $"select Major, Minor, Revision, ScriptNumber from {_versionTableName}";
                    command.Transaction = transaction;

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) throw new CannotReadDatabaseVersionException();
                        var major = reader.GetInt32(0);
                        var minor = reader.GetInt32(1);
                        var revision = reader.GetInt32(2);
                        var scriptNumber = reader.GetInt32(3);
                        return new DatabaseVersion(major, minor, revision, scriptNumber);
                    }
                }
            }
            catch (CannotReadDatabaseVersionException)
            {
                throw;
            }
            catch {}

            return new DatabaseVersion(0, 0, 0, 0);
        }

        private void _UpdateDatabaseVersion(IDbConnection dbConnection, IDbTransaction transaction, List<string> changeScriptSqlFiles)
        {
            var lastChangeScriptVersion = _GetChangeScriptVersionFromFullFileName(changeScriptSqlFiles.Last());
            var databaseVersionOfLastChangeScript = new DatabaseVersion(lastChangeScriptVersion);

            using (var command = dbConnection.CreateCommand())
            {
                command.CommandText = $"update {_versionTableName} set " +
                                      $"    Major = {databaseVersionOfLastChangeScript.Major}, " +
                                      $"    Minor = {databaseVersionOfLastChangeScript.Minor}, " +
                                      $"    Revision = {databaseVersionOfLastChangeScript.Revision}, " +
                                      $"    ScriptNumber = {databaseVersionOfLastChangeScript.ScriptNumber}";
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
        }

        private void _ApplyOtherScripts(string folderWithSqlFiles, IDbConnection dbConnection, IDbTransaction transaction)
        {
            var folders = Directory.GetDirectories(folderWithSqlFiles)
                .OrderBy(x => x)
                .ToList();
            var sqlScriptFiles = Directory.GetFiles(folderWithSqlFiles, _sqlScriptFileSearchPattern).OrderBy(x => x).ToList();

            foreach (var sqlScriptFile in sqlScriptFiles)
            {
                _ApplyOneSqlScript(sqlScriptFile, dbConnection, transaction);
            }

            foreach (var folder in folders)
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
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
