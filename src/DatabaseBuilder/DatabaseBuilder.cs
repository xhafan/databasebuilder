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
        private readonly string _scriptFileSearchPattern;

        public DatabaseBuilder(
            string versionTableName = "Version", 
            string changeScriptsFolderName = "ChangeScripts",
            string otherScriptsFolderName = "OtherScripts",
            string scriptFileSearchPattern = "*.sql"
            )
        {
            _scriptFileSearchPattern = scriptFileSearchPattern;
            _versionTableName = versionTableName;
            _changeScriptsFolderName = changeScriptsFolderName;
            _otherScriptsFolderName = otherScriptsFolderName;
        }

        public void UpgradeDatabase(string folderWithSqlFiles, IDbConnection dbConnection, IDbTransaction transaction)
        {
            _ApplyChangeScripts($"{folderWithSqlFiles}\\{_changeScriptsFolderName}", dbConnection, transaction);
            _ApplyOtherScripts($"{folderWithSqlFiles}\\{_otherScriptsFolderName}", dbConnection, transaction);

        }

        private void _ApplyChangeScripts(string folderWithSqlFiles, IDbConnection dbConnection, IDbTransaction transaction)
        {
            var changeScriptSqlFiles = Directory.GetFiles(folderWithSqlFiles, _scriptFileSearchPattern).OrderBy(x => x).ToList(); // todo: order files by version number
    
            foreach (var changeScriptSqlFile in changeScriptSqlFiles)
            {
                _ApplyOneSqlScript(changeScriptSqlFile, dbConnection, transaction);
            }

            _UpdateDatabaseVersion(dbConnection, transaction, changeScriptSqlFiles);
        }

        private void _UpdateDatabaseVersion(IDbConnection dbConnection, IDbTransaction transaction, List<string> changeScriptSqlFiles)
        {
            var databaseVersionOfLastChangeScript = new DatabaseVersion(changeScriptSqlFiles.Last());

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
            var folders = Directory.GetDirectories(folderWithSqlFiles) // todo: remove change script folder
                .OrderBy(x => x)
                .ToList();
            var sqlScriptFiles = Directory.GetFiles(folderWithSqlFiles, "*.sql").OrderBy(x => x).ToList(); // todo: order files by version number

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
