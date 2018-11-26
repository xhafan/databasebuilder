using System;

namespace DatabaseBuilder
{
    /// <summary>
    /// The exception thrown when cannot read the database table version when the version table exists.
    /// </summary>
    public class CannotReadDatabaseVersionException : Exception
    {
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="message">Exception message</param>
        public CannotReadDatabaseVersionException(string message)
            : base(message)
        {
            
        }
    }
}