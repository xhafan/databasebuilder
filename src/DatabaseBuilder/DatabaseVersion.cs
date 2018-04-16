using System;

namespace DatabaseBuilder
{
    public class DatabaseVersion
    {
        protected DatabaseVersion() {}

        public DatabaseVersion(string changeScriptFileFullName)
        {
            var indexOfLastBackslash = changeScriptFileFullName.LastIndexOf("\\", StringComparison.Ordinal);
            var changeScriptFileName = changeScriptFileFullName.Substring(indexOfLastBackslash + 1);
            var splitResult = changeScriptFileName.Split('.');
            if (splitResult.Length != 5) throw new Exception($"Invalid change script name file name: {changeScriptFileName}");

            Major = int.Parse(splitResult[0]);
            Minor = int.Parse(splitResult[1]);
            Revision = int.Parse(splitResult[2]);
            ScriptNumber = int.Parse(splitResult[3]);
        }

//        public DatabaseVersion(int major, int minor, int revision, int scriptNumber)
//        {
//            Major = major;
//            Minor = minor;
//            Revision = revision;
//            ScriptNumber = scriptNumber;
//        }

        public int Major { get; }
        public int Minor { get; }
        public int Revision { get; }
        public int ScriptNumber { get; }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Revision}.{ScriptNumber}";
        }
    }
}