//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 £ukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using SdfGlueEditor.Platform;
using SdfGlueCore.Model;
using System.Diagnostics;

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

            // redirect 'Console' to Log window
            TextWriter writer = new StringWriter(DataModel.OutputLog);
            TextWriter writerOryginal = Console.Out;
            Console.SetOut(writer);

            // redirect 'Debug' to Log window
            // .Net Framework:
            //Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            // .Net Core:
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            Debug.AutoFlush = true;

            GameWindowSettings gameWindowSettings = new GameWindowSettings();
            NativeWindowSettings nativeWindowSettings = new NativeWindowSettings();

            //gameWindowSettings.RenderFrequency = 60;
            //gameWindowSettings.UpdateFrequency = 60;

            // APIVersion - Remarks:
            //     OpenGL 3.3 is selected by default, and runs on almost any hardware made within
            //     the last ten years. This will run on Windows, Mac OS, and Linux.
            //     OpenGL 4.1 is suggested for modern apps meant to run on more modern hardware.
            //     This will run on Windows, Mac OS, and Linux.
            //     OpenGL 4.6 is suggested for modern apps that only intend to run on Windows and
            //     Linux; Mac OS doesn't support it.
            //     Note that if you choose an API other than base OpenGL, this will need to be updated
            //     accordingly, as the versioning of OpenGL and OpenGL ES do not match.

            //nativeWindowSettings.APIVersion = new Version(3, 2); // to jest najmniejsza supportowana wartoœæ
            nativeWindowSettings.APIVersion = new Version(3, 3);
            //nativeWindowSettings.APIVersion = new Version(4, 1);
            //nativeWindowSettings.APIVersion = new Version(4, 6);

            float windowSizeMul = 0.9f;

            Vector2i defaultScreenRes = PlatformAndOs.GetDefaultScreenResolution();

            int windowSizeX = (int)(defaultScreenRes.X  * windowSizeMul);
            int windowSizeY = (int)(defaultScreenRes.Y * windowSizeMul);

            int windowPosX = (int)((defaultScreenRes.X  - windowSizeX) * 0.5);
            //int windowPosY = (int)((defaultScreenRes.Y - windowSizeY) * 0.25);    // bli¿ej górnej listwy
            int windowPosY = (int)((defaultScreenRes.Y - windowSizeY) * 0.5);

            // Uwaga: po³o¿enie w Y zdaje siê nie uwzglêdniaæ gruboœci górnej belki
            // (przynajmniej tak jest po przejœciu na .net core i OpenTK 4)
            nativeWindowSettings.Location = new Vector2i(windowPosX, windowPosY);
            nativeWindowSettings.Size = new Vector2i(windowSizeX, windowSizeY);

            //Console.WriteLine("DisplayDevice: {0}x{1}", defaultScreenRes.X, defaultScreenRes.Y);
            //Console.WriteLine("Win size: {0}x{1}", defaultWinSizeX, defaultWinSizeY);

            if (UseMainLoopTryCatch)
            {
                try
                {
                    EditorMainWindow wnd = new EditorMainWindow(gameWindowSettings, nativeWindowSettings);
                    wnd.Run();

                    Debug.WriteLine("Application closed.");
                    File.WriteAllText("log.txt", DataModel.OutputLog.ToString());
                }
                catch(Exception ex)
                {
                    Console.WriteLine("[ERROR] Exception in main thread:");
                    Console.WriteLine(ex.ToString());
                    File.WriteAllText("errorlog.txt", DataModel.OutputLog.ToString());
                }
            }
            else
            {
                EditorMainWindow wnd = new EditorMainWindow(gameWindowSettings, nativeWindowSettings);
                wnd.Run();
            }

            writer.Close();
            Console.SetOut(writerOryginal);
        }


    }
}
