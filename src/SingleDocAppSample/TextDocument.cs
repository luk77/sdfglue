using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;

namespace SingleDocAppSample
{
    // The document of the sample application: plain text stored in a .txt file
    public class TextDocument : IAbstractDocument
    {
        public  ExString                Text                    = new ExString("");
        public  string?                 FilePath                = null;

        public string GetText()
        {
            return Text.Val ?? "";
        }

        public void Load(string filePath)
        {
            Text        = new ExString(File.ReadAllText(filePath));
            FilePath    = filePath;
        }

        public void Save(string filePath)
        {
            File.WriteAllText(filePath, GetText());
            FilePath    = filePath;
        }
    }
}
