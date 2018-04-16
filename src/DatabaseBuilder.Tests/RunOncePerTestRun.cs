using System;
using System.Threading;
using NUnit.Framework;

namespace DatabaseBuilder.Tests
{
    [SetUpFixture]
    public class RunOncePerTestRun
    {
        private Mutex _mutex;

        [OneTimeSetUp]
        public void SetUp()
        {
            _acquireSynchronizationMutex();

            void _acquireSynchronizationMutex()
            {
                var mutexName = GetType().Namespace;
                _mutex = new Mutex(false, mutexName);
                if (!_mutex.WaitOne(TimeSpan.FromSeconds(60)))
                {
                    throw new Exception(
                        "Timeout waiting for synchronization mutex to prevent other .net frameworks running concurrent tests over the same database");
                }
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }
}