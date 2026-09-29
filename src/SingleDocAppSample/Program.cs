using SingleDocAppFramework;
using SingleDocAppFramework.Platform;
using SingleDocAppPlatformWin;

namespace SingleDocAppSample
{
    // Minimal application built only on SingleDocAppCore + SingleDocAppFramework:
    // a plain text editor with File/Edit/View/Layouts menus, undo/redo, layouts and a log window.
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            SdAppLaunchOptions options = new SdAppLaunchOptions();
            options.ParseCommandLine(args);
            options.WindowSizeFactor = 0.7f;

            IPlatformServices platform = new WinPlatformServices();

            SdAppLauncher.Run(options, platform, (gameWindowSettings, nativeWindowSettings) =>
                new SampleAppWindow(gameWindowSettings, nativeWindowSettings, platform));
        }
    }
}
