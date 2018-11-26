using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace DatabaseBuilder.Tests
{
    [TestFixture]
    public class when_building_existing_database : BaseBuildingDatabase
    {
        [SetUp]
        public void Context()
        {
            var builderOfDatabase = new BuilderOfDatabase(GetDbConnection);

            DropDatabaseObjectsToMakeDatabaseEmpty();

            builderOfDatabase.BuildDatabase(SqlFilesDirectoryPath);

            CreateNewChangeScript();

            builderOfDatabase.BuildDatabase(SqlFilesDirectoryPath);
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
            var text = ExecuteSqlQuery<string>("select Text2 from \"DataTable\" where Id = 1").Single();
            text.ShouldBe("some other text");
        }
    }
}