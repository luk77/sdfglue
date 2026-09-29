//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppFramework;
using SingleDocAppFramework.Platform;
using SingleDocAppPlatformWin;

namespace SdfGlueEditor
{
    static class Program
    {
        public static readonly bool                 UseMainLoopTryCatch         = false;

        [STAThread]
        static void Main(string[] args)
        {
            // debug sleep
            //Thread.Sleep(20000);

            SdAppLaunchOptions options = new SdAppLaunchOptions();
            options.ParseCommandLine(args);
            options.APIVersion              = new Version(3, 3);
            options.WindowSizeFactor        = 0.9f;
            options.RedirectConsoleToLog    = true;
            options.CatchMainLoopExceptions = UseMainLoopTryCatch;

            IPlatformServices platform = new WinPlatformServices();

            SdAppLauncher.Run(options, platform, (gameWindowSettings, nativeWindowSettings) =>
                new EditorMainWindow(gameWindowSettings, nativeWindowSettings, platform));
        }
    }
}
