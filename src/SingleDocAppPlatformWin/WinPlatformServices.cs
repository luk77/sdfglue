//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using OpenTK.Mathematics;
using NativeWindow = OpenTK.Windowing.Desktop.NativeWindow;
using SingleDocAppFramework.Platform;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SingleDocAppPlatformWin
{
    // IPlatformServices implementation for Windows (WinForms + Win32)
    public class WinPlatformServices : IPlatformServices
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);


        public float ObtainDpiScaling()
        {
            float windowsScaling = 1.0f;
            try
            {
                int defaultDpi = 96;

                // https://stackoverflow.com/questions/32607468/get-scale-of-screen
                // Platform-specific and may be unreliable!!! Relies on a registry key!
                object? regVal = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop", "LogPixels", defaultDpi);
                int currentDPI = regVal as int? ?? defaultDpi;
                float scaling = defaultDpi / (float)currentDPI;
                windowsScaling = 1.0f / scaling;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ObtainDpiScaling() failed. Exception: {0}", ex.ToString());
            }

            return windowsScaling;
        }

        public Vector2i GetPrimaryScreenWorkingArea()
        {
            // Note: SystemInformation.VirtualScreen does not work well with two monitors and an extended
            // desktop (it returns the size of the combined screen).

            // default - in case no monitor is found... ;)
            int screenResX = 1920;
            int screenResY = 1080;
            foreach(Screen screen in Screen.AllScreens)
            {
                if (screen.Primary)
                {
                    screenResX = screen.WorkingArea.Width;
                    screenResY = screen.WorkingArea.Height;
                }
            }

            return new Vector2i(screenResX, screenResY);
        }

        /// <summary>Returns true if the current application has focus, false otherwise</summary>
        public bool IsApplicationFocused(NativeWindow window)
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

        public string? OpenFileDialog(FileFilter filter, string? initialDirectory = null)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                SetupFileDialog(dlg, filter, initialDirectory);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.FileName;
                }
            }

            return null;
        }

        public string? SaveFileDialog(FileFilter filter, string? initialDirectory = null)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                SetupFileDialog(dlg, filter, initialDirectory);

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.FileName;
                }
            }

            return null;
        }

        public string? SelectFolderDialog()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.SelectedPath;
                }
            }

            return null;
        }

        public void ShowErrorMessage(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public bool AskOkCancel(string title, string message)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK;
        }

        public DialogAnswer AskYesNoCancel(string title, string message)
        {
            switch (MessageBox.Show(message, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning))
            {
                case DialogResult.Yes:  return DialogAnswer.Yes;
                case DialogResult.No:   return DialogAnswer.No;
                default:                return DialogAnswer.Cancel;
            }
        }

        private static void SetupFileDialog(FileDialog dlg, FileFilter filter, string? initialDirectory)
        {
            dlg.Filter              = String.Format("{0} ({1})|{1}|All files (*.*)|*.*", filter.Description, filter.Pattern);
            dlg.FilterIndex         = 1;
            dlg.RestoreDirectory    = true;
            dlg.AddExtension        = true;
            dlg.DefaultExt          = filter.GetExtension().TrimStart('.');

            // WinForms requires an absolute path
            if (!String.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                dlg.InitialDirectory = Path.GetFullPath(initialDirectory);
        }
    }
}
