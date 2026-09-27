using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace SingleDocAppFramework.Platform
{
    // OS-dependent services. The implementation is provided by the application
    // (e.g. SingleDocAppPlatformWin.WinPlatformServices) or DefaultPlatformServices is used.
    public interface IPlatformServices
    {
        float       ObtainDpiScaling            ();
        bool        IsApplicationFocused        (NativeWindow window);
        Vector2i    GetPrimaryScreenWorkingArea ();

        // Return null when the user cancelled the dialog (or dialogs are not supported)
        string?     OpenFileDialog              (FileFilter filter, string? initialDirectory = null);
        string?     SaveFileDialog              (FileFilter filter, string? initialDirectory = null);
        string?     SelectFolderDialog          ();
    }
}
