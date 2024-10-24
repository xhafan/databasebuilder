using System;

namespace DatabaseBuilder;

/// <summary>
/// The exception thrown when cannot update the database version; usually due to concurrent update by other process.
/// </summary>
public class CannotUpdateVersionException : Exception
{
    /// <summary>
    /// Initializes the instance.
    /// </summary>
    /// <param name="message">Exception message</param>
    public CannotUpdateVersionException(string message)
        : base(message)
    {
            
    }
}