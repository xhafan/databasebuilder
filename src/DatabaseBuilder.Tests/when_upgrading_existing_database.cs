using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace DatabaseBuilder.Tests
{
    [TestFixture]
    public class when_upgrading_existing_database : BaseUpgradingDatabase
    {
        [SetUp]
        public void Context()
        {
            var databaseBuilder = new DatabaseBuilder();

            ExecuteWithinTransaction((connection, transaction) =>
            {
                DropDatabaseObjectsToMakeDatabaseEmpty();
                databaseBuilder.UpgradeDatabase(FolderWithSqlFiles, connection, transaction);
            });

            CreateNewChangeScript();

            ExecuteWithinTransaction((connection, transaction) =>
            {
                databaseBuilder.UpgradeDatabase(FolderWithSqlFiles, connection, transaction);
            });
        }

        [Test]
        public void database_version_is_upgraded()
        {
            var currentDatabaseVersion = GetCurrentDatabaseVersion();
            currentDatabaseVersion.ToString().ShouldBe("1.0.0.10");
        }

        [Test]
        public void new_change_script_is_applied()
        {
            var text = ExecuteSqlQuery<string>("select Text2 from DataTable where Id = 1").Single();
            text.ShouldBe("some other text");
        }
    }
}