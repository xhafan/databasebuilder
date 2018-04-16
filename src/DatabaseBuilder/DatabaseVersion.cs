using System;

namespace DatabaseBuilder
{
    public class DatabaseVersion : IComparable<DatabaseVersion>
    {
        protected DatabaseVersion() {}

        public DatabaseVersion(string version)
        {
            var splitResult = version.Split('.');
            if (splitResult.Length < 4) throw new Exception($"Invalid version: {version}");

            Major = int.Parse(splitResult[0]);
            Minor = int.Parse(splitResult[1]);
            Revision = int.Parse(splitResult[2]);
            ScriptNumber = int.Parse(splitResult[3]);
        }

        public DatabaseVersion(int major, int minor, int revision, int scriptNumber)
        {
            Major = major;
            Minor = minor;
            Revision = revision;
            ScriptNumber = scriptNumber;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Revision { get; }
        public int ScriptNumber { get; }

        public int CompareTo(DatabaseVersion other)
        {
            return new Version(Major, Minor, Revision, ScriptNumber)
                .CompareTo(new Version(other.Major, other.Minor, other.Revision, other.ScriptNumber));
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Revision}.{ScriptNumber}";
        }   
    }
}