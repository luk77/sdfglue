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

        public  string                  UserSettingsFile        = "UserSettings.xml";   // stored next to the executable

        public  bool                    DevelopmentMode         = false;    // "Development" main menu; set by SdAppLauncher (--devel)

        // Optional input system (keyboard/gamepad/... input channels, SdAppWindow.Inputs).
        // Disabled: nothing is created, no devices are polled and the input settings file is not used.
        public  bool                    EnableInputSystem       = false;
        public  string                  InputSettingsFile       = "InputSettings.xml";  // stored next to the executable

        // Fonts (Ui/Fonts/UiFonts): TTF/OTF files selectable in the user settings (UiFont, CodeFont).
        // A missing directory is not an error - only the built-in ImGui font is available then.
        public  string                  FontsDirectory          = "Fonts";  // relative to the working directory (like LayoutsDirectory)

        // Characters loaded from the TTF/OTF fonts: pairs of inclusive [first, last] code points.
        // Default: Basic Latin, Latin-1 Supplement, Latin Extended-A (e.g. Polish), General Punctuation (quotes, dashes, ellipsis).
        // Applications may add ranges (e.g. Cyrillic 0x0400-0x04FF). Characters outside the ranges are displayed as '?'.
        public  ushort[]                FontGlyphRanges         = { 0x0020, 0x00FF, 0x0100, 0x017F, 0x2000, 0x206F };

        public string GetDefaultLayoutPath()
        {
            return Path.Combine(LayoutsDirectory, DefaultLayoutFile);
        }

        public string GetUserSettingsPath()
        {
            return Path.Combine(AppContext.BaseDirectory, UserSettingsFile);
        }

        public string GetInputSettingsPath()
        {
            return Path.Combine(AppContext.BaseDirectory, InputSettingsFile);
        }
    }
}
