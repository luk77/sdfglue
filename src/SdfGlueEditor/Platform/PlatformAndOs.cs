//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SdfGlueEditor.Platform
{
    public static class PlatformAndOs
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);


        public static float ObtainWindowsScaling()
        {

            float windowsScaling = 1.0f;
            try
            {
                // .Net Framework:
                //windowsScaling = (float)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / ((float)System.Windows.SystemParameters.PrimaryScreenWidth);

                int defaultDpi = 96;

                // .Net Core:
                //windowsScaling = 2.5f;     // TODO: (CORE)
                // https://stackoverflow.com/questions/32607468/get-scale-of-screen
                // To jest platform-specific i może być zawodne!!! Polegamy na kluczu w rejestrze!
                object? regVal = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop", "LogPixels", defaultDpi);
                int currentDPI = regVal as int? ?? defaultDpi;
                float scaling = defaultDpi / (float)currentDPI;
                windowsScaling = 1.0f / scaling;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ObtainWindowsScaling() failed. Exception: {0}", ex.ToString());
            }

            return windowsScaling;
        }

        public static Vector2i GetDefaultScreenResolution()
        {
            // .Net Framework:
            //return new Vector2i(DisplayDevice.Default.Width, DisplayDevice.Default.Height);

            // .Net Core:
            /*
            // TODO: (CORE): to jest bardzo zawodna metoda wyciągnięcia rozdzielczości bieżącego ekranu!
            // VideoController2 - to jest druga karta graficzna (czyli GeForce, a nie zintegrowana)
            //https://stackoverflow.com/questions/71537840/get-current-screen-resolution-using-net-core-console-application
            //ManagementObject wmiVideoController = new ManagementObject("Win32_VideoController.DeviceID=\"VideoController1\"");
            ManagementObject wmiVideoController = new ManagementObject("Win32_VideoController.DeviceID=\"VideoController2\"");
            //ManagementObject wmiDesktopMonitor = new ManagementObject("Win32_DesktopMonitor.DeviceID=\"DesktopMonitor1\"");
            uint vidCtrlSizeX = (uint)wmiVideoController["CurrentHorizontalResolution"];
            uint vidCtrlSizeY = (uint)wmiVideoController["CurrentVerticalResolution"];
            //UInt32 deskMonSizeX = (UInt32)wmiDesktopMonitor["ScreenWidth"];
            //UInt32 deskMonSizeY = (UInt32)wmiDesktopMonitor["ScreenHeight"]; 
            //return new Vector2i((int)vidCtrlSizeX, (int)vidCtrlSizeY);
            */

            // .Net Core:

            // To niestety słabo się sprawdza w przypadku dwóch monitorów z rozszerzonym pulpit'em
            // Zwracana jest łączna wartość połączonego ekranu...
            //double screenResX = SystemInformation.VirtualScreen.Width;
            //double screenResY = SystemInformation.VirtualScreen.Height;

            // To tylko sztuczka, ale raczej nie pokryje wszystkich przypadków...
            //double screenResX = SystemInformation.VirtualScreen.Width + SystemInformation.VirtualScreen.X;
            //double screenResY = SystemInformation.VirtualScreen.Height + SystemInformation.VirtualScreen.Y;

            // TODO: to jest tymczasowe rozwiązanie...
            //double screenResX = Math.Min(1920, SystemInformation.VirtualScreen.Width);
            //double screenResY = Math.Min(1080, SystemInformation.VirtualScreen.Height);
            //return new Vector2i((int)screenResX, (int)screenResY);

			// To działa ok:
            // default - gdyby nie znaleziono monitora... ;)
            int screenResX = 1920;
            int screenResY = 1080;
            foreach(Screen screen in Screen.AllScreens)
            {
                //Console.WriteLine("Device Name: " + screen.DeviceName);
                //Console.WriteLine("Bounds: " + screen.Bounds.ToString());
                //Console.WriteLine("Type: " + screen.GetType().ToString());
                //Console.WriteLine("Working Area: " + screen.WorkingArea.ToString());
                //Console.WriteLine("Primary Screen: " + screen.Primary.ToString());
                if (screen.Primary)
                {
                    screenResX = screen.WorkingArea.Width;
                    screenResY = screen.WorkingArea.Height;
                }
            }

            return new Vector2i(screenResX, screenResY);
        }

        /// <summary>Returns true if the current application has focus, false otherwise</summary>
        public static bool IsApplicationFocused()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false;       // No window is currently activated
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);

            bool appIsActive = activeProcId == procId;

            return appIsActive;
        }


    }
}
