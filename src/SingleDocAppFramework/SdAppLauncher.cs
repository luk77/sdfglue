using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using SingleDocAppFramework.Diagnostics;
using SingleDocAppFramework.Platform;
using System.Diagnostics;

namespace SingleDocAppFramework
{
    public class SdAppLaunchOptions
    {
        // OpenGL version:
        //     3.3 is selected by default, and runs on almost any hardware made within the last ten years
        //         (Windows, Mac OS, Linux).
        //     4.1 is suggested for modern apps meant to run on more modern hardware (Windows, Mac OS, Linux).
        //     4.6 is suggested for modern apps that only intend to run on Windows and Linux.
        //     3.2 is the minimal supported version.
        public  Version                 APIVersion              = new Version(3, 3);

        // Initial window size as a fraction of the primary screen working area (window is centered)
        public  float                   WindowSizeFactor        = 0.9f;

        // Redirect Console and Trace/Debug output to AppLog (displayed by WndLog)
        public  bool                    RedirectConsoleToLog    = true;

        // Catch exceptions from the main loop and save the log to ErrorLogFilePath.
        // On normal exit the log is saved to LogFilePath (only in this mode).
        public  bool                    CatchMainLoopExceptions = false;
        public  string                  LogFilePath             = "log.txt";
        public  string                  ErrorLogFilePath        = "errorlog.txt";

        // Development mode (command line: --devel) - shows the "Development" main menu (SdAppSettings.DevelopmentMode)
        public  bool                    DevelopmentMode         = false;

        public const string             ArgDevelopmentMode      = "--devel";

        // Applies the framework command line options; other arguments are ignored (left to the application)
        public void ParseCommandLine(string[] args)
        {
            foreach (string arg in args)
            {
                if (String.Equals(arg, ArgDevelopmentMode, StringComparison.OrdinalIgnoreCase))
                    DevelopmentMode = true;
            }
        }
    }

    // Creates and runs the application window
    public static class SdAppLauncher
    {
        public static void Run(SdAppLaunchOptions options, IPlatformServices platform, Func<GameWindowSettings, NativeWindowSettings, SdAppWindow> createWindow)
        {
            if (options.RedirectConsoleToLog)
                AppLog.RedirectConsoleAndTrace();

            GameWindowSettings      gameWindowSettings      = new GameWindowSettings();
            NativeWindowSettings    nativeWindowSettings    = CreateNativeWindowSettings(options, platform);

            if (options.CatchMainLoopExceptions)
            {
                try
                {
                    using (SdAppWindow wnd = createWindow(gameWindowSettings, nativeWindowSettings))
                    {
                        ApplyOptionsToWindow(options, wnd);
                        wnd.Run();
                    }

                    Debug.WriteLine("Application closed.");
                    AppLog.SaveTo(options.LogFilePath);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("[ERROR] Exception in main thread:");
                    Console.WriteLine(ex.ToString());
                    AppLog.SaveTo(options.ErrorLogFilePath);
                }
            }
            else
            {
                using (SdAppWindow wnd = createWindow(gameWindowSettings, nativeWindowSettings))
                {
                    ApplyOptionsToWindow(options, wnd);
                    wnd.Run();
                }
            }

            AppLog.Restore();
        }

        // Called before Run() - the UI is created later (OnLoad)
        private static void ApplyOptionsToWindow(SdAppLaunchOptions options, SdAppWindow wnd)
        {
            wnd.AppSettings.DevelopmentMode = options.DevelopmentMode;
        }

        private static NativeWindowSettings CreateNativeWindowSettings(SdAppLaunchOptions options, IPlatformServices platform)
        {
            NativeWindowSettings nativeWindowSettings = new NativeWindowSettings();
            nativeWindowSettings.APIVersion = options.APIVersion;

            Vector2i screenRes = platform.GetPrimaryScreenWorkingArea();

            int windowSizeX = (int)(screenRes.X * options.WindowSizeFactor);
            int windowSizeY = (int)(screenRes.Y * options.WindowSizeFactor);

            // Note: the Y position does not seem to take the title bar height into account
            // (at least after moving to .NET Core and OpenTK 4)
            int windowPosX  = (int)((screenRes.X - windowSizeX) * 0.5);
            int windowPosY  = (int)((screenRes.Y - windowSizeY) * 0.5);

            nativeWindowSettings.Location   = new Vector2i(windowPosX, windowPosY);
            nativeWindowSettings.ClientSize = new Vector2i(windowSizeX, windowSizeY);

            return nativeWindowSettings;
        }
    }
}
