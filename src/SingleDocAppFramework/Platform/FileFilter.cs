namespace SingleDocAppFramework.Platform
{
    // Platform-independent file filter description for file dialogs
    public class FileFilter
    {
        public  string                  Description;    // e.g. "Project files"
        public  string                  Pattern;        // e.g. "*.xml"

        public FileFilter(string description, string pattern)
        {
            Description = description;
            Pattern     = pattern;
        }

        // ".xml" for "*.xml", empty string when there is no extension
        public string GetExtension()
        {
            return Path.GetExtension(Pattern);
        }
    }
}
