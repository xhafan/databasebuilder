using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace DatabaseBuilder.Tests
{
    [TestFixture]
    public class when_building_existing_database_concurrently : BaseBuildingDatabase
    {
        [SetUp]
        public void Context()
        {
            var builderOfDatabase = new BuilderOfDatabase(GetDbConnection);
            DropDatabaseObjectsToMakeDatabaseEmpty();
            builderOfDatabase.BuildDatabase(SqlFilesDirectoryPath);
            CreateNewChangeScript();    

            var taskOne = Task.Factory.StartNew(() =>
            {
                new BuilderOfDatabase(GetDbConnection).BuildDatabase(SqlFilesDirectoryPath);
            });
            var taskTwo = Task.Factory.StartNew(() =>
            {
                new BuilderOfDatabase(GetDbConnection).BuildDatabase(SqlFilesDirectoryPath);
            });
            Task.WaitAll(taskOne, taskTwo);
        }

        [Test]
        public void database_version_is_upgraded()
        {
            var currentDatabaseVersion = GetCurrentDatabaseVersion();
            currentDatabaseVersion.ShouldBe("1.0.0.10");
        }

        [Test]
        public void new_change_script_is_applied()
        {
            var text = ExecuteSqlQuery<string>("select \"Text2\" from \"DataTable\" where \"Id\" = 1").Single();
            text.ShouldBe("some other text");
        }
    }
}