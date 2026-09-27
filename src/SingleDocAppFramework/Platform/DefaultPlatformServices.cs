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
    }
}
