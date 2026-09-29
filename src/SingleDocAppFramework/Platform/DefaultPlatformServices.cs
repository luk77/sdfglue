using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace SingleDocAppFramework.Platform
{
    // Minimal, platform-independent implementation (no file dialogs)
    public class DefaultPlatformServices : IPlatformServices
    {
        public virtual float ObtainDpiScaling()
        {
            return 1.0f;
        }

        public virtual bool IsApplicationFocused(NativeWindow window)
        {
            return window.IsFocused;
        }

        public virtual Vector2i GetPrimaryScreenWorkingArea()
        {
            return new Vector2i(1920, 1080);
        }

        public virtual string? OpenFileDialog(FileFilter filter, string? initialDirectory = null)
        {
            return null;
        }

        public virtual string? SaveFileDialog(FileFilter filter, string? initialDirectory = null)
        {
            return null;
        }

        public virtual string? SelectFolderDialog()
        {
            return null;
        }

        // Without dialogs the messages go to the log and questions get the answer that does not block
        // the operation (as if there was no question)
        public virtual void ShowErrorMessage(string title, string message)
        {
            Console.WriteLine("ERROR: {0}: {1}", title, message);
        }

        public virtual bool AskOkCancel(string title, string message)
        {
            Console.WriteLine("{0}: {1} -> OK (no dialogs)", title, message);
            return true;
        }

        public virtual DialogAnswer AskYesNoCancel(string title, string message)
        {
            Console.WriteLine("{0}: {1} -> No (no dialogs)", title, message);
            return DialogAnswer.No;
        }
    }
}
