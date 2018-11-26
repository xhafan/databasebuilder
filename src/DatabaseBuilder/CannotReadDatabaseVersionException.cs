using System;

namespace DatabaseBuilder
{
    /// <summary>
    /// The exception thrown when cannot read the database version; usually due to missing row with the version info in the version table.
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