using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace DatabaseBuilder.Tests
{
    [TestFixture]
    public class when_building_empty_database : BaseBuildingDatabase
    {
        [SetUp]
        public void Context()
        {
            var builderOfDatabase = new BuilderOfDatabase(GetDbConnection);

            DropDatabaseObjectsToMakeDatabaseEmpty();

            builderOfDatabase.BuildDatabase(SqlFilesDirectoryPath);
        }

        [Test]
        public void database_version_is_upgraded()
        {
            var currentDatabaseVersion = GetCurrentDatabaseVersion();
            currentDatabaseVersion.ShouldBe("1.0.0.2");
        }

        [Test]
        public void database_view_was_created()
        {
            var text = ExecuteSqlQuery<string>("select Text from \"DataTableDto\" where Id = 1").Single();
            text.ShouldBe("some text");
        }
    }
}
