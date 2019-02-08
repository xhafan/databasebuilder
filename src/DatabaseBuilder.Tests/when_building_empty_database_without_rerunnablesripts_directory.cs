using System.IO;
using NUnit.Framework;
using Shouldly;

namespace DatabaseBuilder.Tests
{
    [TestFixture]
    public class when_building_empty_database_without_rerunnablesripts_directory : BaseBuildingDatabase
    {
        private string _reRunnableScriptsDirectoryPath;
        private string _hiddenReRunnableScriptsDirectoryPath;
        const string ReRunnableScriptsDirectoryName = "ReRunnableScripts";

        [SetUp]
        public void Context()
        {
            var builderOfDatabase = new BuilderOfDatabase(GetDbConnection, reRunnableScriptsDirectoryName: ReRunnableScriptsDirectoryName);

            DropDatabaseObjectsToMakeDatabaseEmpty();

            _hideReRunnableScriptsDirectory();

            builderOfDatabase.BuildDatabase(SqlFilesDirectoryPath);

            void _hideReRunnableScriptsDirectory()
            {
                _reRunnableScriptsDirectoryPath = Path.Combine(SqlFilesDirectoryPath, ReRunnableScriptsDirectoryName);
                _hiddenReRunnableScriptsDirectoryPath = Path.Combine(SqlFilesDirectoryPath, ReRunnableScriptsDirectoryName + "_HIDDEN");

                Directory.Move(_reRunnableScriptsDirectoryPath, _hiddenReRunnableScriptsDirectoryPath);
            }
        }

        [Test]
        public void database_version_is_upgraded()
        {
            var currentDatabaseVersion = GetCurrentDatabaseVersion();
            currentDatabaseVersion.ShouldBe("1.0.0.2");
        }

        [TearDown]
        public void TearDown()
        {
            _restoreReRunnableScriptsDirectory();

            void _restoreReRunnableScriptsDirectory()
            {
                Directory.Move(_hiddenReRunnableScriptsDirectoryPath, _reRunnableScriptsDirectoryPath);
            }
        }
    }
}