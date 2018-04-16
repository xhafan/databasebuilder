using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace DatabaseBuilder.Tests
{
    [TestFixture]
    public class when_upgrading_empty_database : BaseUpgradingDatabase
    {
        [SetUp]
        public void Context()
        {
            var databaseBuilder = new DatabaseBuilder();

            ExecuteWithinTransaction((connection, transaction) =>
            {
                DropDatabaseObjectsToMakeDatabaseEmpty();                
                databaseBuilder.UpgradeDatabase(_folderWithSqlFiles, connection, transaction);
            });
        }

        [Test]
        public void database_version_is_upgraded()
        {
            var currentDatabaseVersion = GetCurrentDatabaseVersion();
            currentDatabaseVersion.ToString().ShouldBe("1.0.0.2");
        }

        [Test]
        public void database_view_was_created()
        {
            var text = ExecuteSqlQuery<string>("select Text from DataTableDto where Id = 1").Single();
            text.ShouldBe("some text");
        }
    }
}
