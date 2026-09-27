using SingleDocAppFramework.Platform;

namespace SingleDocAppFramework
{
    // Framework settings provided by the concrete application
    public class SdAppSettings
    {
        public  string                  AppName                 = "SingleDocApp";

        public  string                  LayoutsDirectory        = "Layouts";
        public  string                  DefaultLayoutFile       = "Default.xml";
        public  FileFilter              LayoutFileFilter        = new FileFilter("Layout files", "*.xml");

        public  FileFilter              DocumentFileFilter      = new FileFilter("Document files", "*.xml");

        public  bool                    EnableDocking           = true;
        public  bool                    EnableViewports         = false;    // allows dragging ImGui windows out of the main window

        public  bool                    EnableFrameworkShortcuts = true;    // Ctrl+N/O/S/Z/Y/W, Ctrl+1..0

        public string GetDefaultLayoutPath()
        {
            return Path.Combine(LayoutsDirectory, DefaultLayoutFile);
        }
    }
}
