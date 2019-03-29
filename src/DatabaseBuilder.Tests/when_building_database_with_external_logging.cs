using System;
using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace DatabaseBuilder.Tests
{
    [TestFixture]
    public class when_building_database_with_external_logging : BaseBuildingDatabase
    {
        private List<string> _loggedMessages;

        [SetUp]
        public void Context()
        {
            _loggedMessages = new List<string>();
            Action<string> logAction = loggedMessage => _loggedMessages.Add(loggedMessage);

            var builderOfDatabase = new BuilderOfDatabase(GetDbConnection, logAction: logAction);

            DropDatabaseObjectsToMakeDatabaseEmpty();

            builderOfDatabase.BuildDatabase(SqlFilesDirectoryPath);
        }

        [Test]
        public void correct_messages_are_logged()
        {
            _loggedMessages.ShouldBe(new[]
            {
                "Current database version: 0.0.0.0",
                "Change scripts applied:",
                "1.0.0.1",
                "1.0.0.2",
                "Database version updated to 1.0.0.2"
            });
        }
    }
}