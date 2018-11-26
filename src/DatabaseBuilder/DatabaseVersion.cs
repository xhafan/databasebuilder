using System;

namespace DatabaseBuilder
{
    /// <summary>
    /// Represents a database version in the following format 'Major.Minor.Revision.ScriptNumber'.
    /// Example: '1.2.3.4' - 1 major, 2 minor, 3 revision, 4 script number.
    /// </summary>
    public class DatabaseVersion : IComparable<DatabaseVersion>
    {
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="version">Database version encoded in a string, e.g. '1.2.3.4' (1 major, 2 minor, 3 revision, 4 script number)</param>
        public DatabaseVersion(string version)
        {
            var splitResult = version.Split('.');
            if (splitResult.Length < 4) throw new Exception($"Invalid version: {version}");

            Major = int.Parse(splitResult[0]);
            Minor = int.Parse(splitResult[1]);
            Revision = int.Parse(splitResult[2]);
            ScriptNumber = int.Parse(splitResult[3]);
        }

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="major">Major number</param>
        /// <param name="minor">Minor number</param>
        /// <param name="revision">Revision number</param>
        /// <param name="scriptNumber">Script number</param>
        public DatabaseVersion(int major, int minor, int revision, int scriptNumber)
        {
            Major = major;
            Minor = minor;
            Revision = revision;
            ScriptNumber = scriptNumber;
        }

        /// <summary>
        /// Gets the major number.
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Gets the minor number.
        /// </summary>
        public int Minor { get; }
        
        /// <summary>
        /// Gets the revision number.
        /// </summary>
        public int Revision { get; }
        
        /// <summary>
        /// Gets the script number.
        /// </summary>
        public int ScriptNumber { get; }

        /// <summary>
        /// Compares database versions.
        /// </summary>
        /// <param name="other">Other database version for the comparison</param>
        /// <returns>Less than zero - the database version is smaller; zero - the database versions are equal;
        /// greater than zero - the current database version is higher</returns>
        public int CompareTo(DatabaseVersion other)
        {
            return new Version(Major, Minor, Revision, ScriptNumber)
                .CompareTo(new Version(other.Major, other.Minor, other.Revision, other.ScriptNumber));
        }

        /// <summary>
        /// Returns a string version of the database version, e.g. '1.2.3.4' (1 major, 2 minor, 3 revision, 4 script number)
        /// </summary>
        /// <returns>A string version of the database version</returns>
        public override string ToString()
        {
            return $"{Major}.{Minor}.{Revision}.{ScriptNumber}";
        }   
    }
}