using ImGuiNET;
using SingleDocAppFramework.Layouts;
using SingleDocAppFramework.Ui.Menu;
using SingleDocAppFramework.Ui.Properties;
using SingleDocAppFramework.Ui.Settings;

namespace SingleDocAppFramework.Ui
{
    public abstract class UiManagerBase
    {
        public  float                   WindowsScaling          = 1.0f;
        public  int                     MainWindowSizeX         = 0;
        public  int                     MainWindowSizeY         = 0;

        public  bool                    ShowImGuiDemoWindow     = false;

        public  SdAppSettings           AppSettings             = new SdAppSettings();    // set by SdAppWindow

        private IUiExecutorFramework    executor_;
        private MainMenuBase?           mainMenu_               = null;
        private SettingsDialog?         settingsDialog_         = null;     // created lazily (RegisterSettingsCategories)

        private List<UiWindowBase>      allWindows_             = new List<UiWindowBase>();
        private List<string>            viewGroups_             = new List<string>();     // in registration order

        protected UiManagerBase(IUiExecutorFramework executor)
        {
            executor_ = executor;
        }

        public IUiExecutorFramework Executor { get { return executor_; } }

        protected List<UiWindowBase> GetAllWindows() { return allWindows_; }

        // viewGroup: null - window listed directly in the "View" menu,
        //            otherwise - window listed in the "View / <viewGroup>" submenu
        protected UiWindowBase RegisterWindow(UiWindowBase window, string? viewGroup = null, bool visibleOnStart = true)
        {
            window.IsVisible = visibleOnStart;
            window.ViewGroup = viewGroup;
            window.SetUiManager(this);
            allWindows_.Add(window);

            if (viewGroup != null && !viewGroups_.Contains(viewGroup))
                viewGroups_.Add(viewGroup);

            return window;
        }

        public IReadOnlyList<string> GetViewGroups()
        {
            return viewGroups_;
        }

        public IEnumerable<UiWindowBase> GetWindowsInViewGroup(string? viewGroup)
        {
            return allWindows_.Where(wnd => wnd.ViewGroup == viewGroup);
        }

        public WindowsVisibilityCollection GetWindowsVisibility()
        {
            WindowsVisibilityCollection windowsVisibility = new WindowsVisibilityCollection();
            foreach(UiWindowBase wnd in allWindows_)
                windowsVisibility.Add(wnd.Title, wnd.IsVisible);

            return windowsVisibility;
        }

        public void ToggleWindow(int windowIndex)
        {
            if (windowIndex < 0)
                return;

            if (windowIndex >= allWindows_.Count)
                return;

            allWindows_[windowIndex].IsVisible = !allWindows_[windowIndex].IsVisible;
        }

        public UiWindowBase? FindFocusedWindow()
        {
            foreach(UiWindowBase wnd in allWindows_)
            {
                if (wnd.IsFocused)
                    return wnd;
            }

            return null;
        }

        public void CloseCurrentWindow()
        {
            UiWindowBase? wnd = FindFocusedWindow();
            if (wnd == null)
                return;

            wnd.IsVisible = false;
        }

        public UiWindowBase? FindWindowByName(string wndName)
        {
            foreach(UiWindowBase wnd in allWindows_)
            {
                if (wndName == wnd.Title)
                    return wnd;
            }

            return null;
        }

        // Override to handle input of UI elements that are not registered windows (call the base implementation)
        public virtual void HandleInput(float deltaTime)
        {
            foreach(UiWindowBase wnd in allWindows_)
            {
                wnd.HandleInput(deltaTime);
            }
        }

        // Override to provide an application-specific main menu (derived from MainMenuBase)
        protected virtual MainMenuBase CreateMainMenu()
        {
            return new MainMenuBase(this);
        }

        // Override to replace the whole UI in some modes (e.g. full screen preview).
        // Return true if the standard UI (menu + windows) should not be built this frame.
        protected virtual bool SubmitOverrideUI()
        {
            return false;
        }

        // Override to change the flags of the dockspace over the main viewport, e.g. PassthruCentralNode
        // (the empty central node shows what the application rendered under the UI) or KeepAliveOnly
        // (the dockspace is not displayed, docked windows stay docked)
        protected virtual ImGuiDockNodeFlags GetDockSpaceFlags()
        {
            return ImGuiDockNodeFlags.None;
        }

        // Categories of the settings dialog (File > Settings...). The base implementation registers the framework
        // categories (FrameworkSettingsCategories). Override to add application categories or to extend
        // the framework ones (call the base implementation first); with no categories the dialog is not available.
        protected virtual void RegisterSettingsCategories(SettingsRegistry registry)
        {
            FrameworkSettingsCategories.Register(registry);
        }

        // created lazily, so the derived class is fully constructed before RegisterSettingsCategories() is called
        private SettingsDialog GetSettingsDialog()
        {
            if (settingsDialog_ == null)
            {
                SettingsRegistry registry = new SettingsRegistry();
                RegisterSettingsCategories(registry);
                settingsDialog_ = new SettingsDialog(executor_, registry);
            }
            return settingsDialog_;
        }

        public bool IsSettingsDialogAvailable()
        {
            return GetSettingsDialog().IsAvailable;
        }

        public void OpenSettingsDialog()
        {
            GetSettingsDialog().Open();
        }

        // True while a modal dialog is open - application shortcuts and window input are disabled
        public bool IsModalDialogOpen()
        {
            return (settingsDialog_ != null && settingsDialog_.IsOpen) || IsAppModalDialogOpen();
        }

        // Override to build application modal dialogs (ImGui popups). Called after the windows and the settings dialog,
        // also when SubmitOverrideUI() replaced the standard UI.
        protected virtual void BuildAppModalDialogs()
        {
        }

        // Override to report the application modal dialogs built in BuildAppModalDialogs() (see IsModalDialogOpen())
        protected virtual bool IsAppModalDialogOpen()
        {
            return false;
        }

        public void SubmitUI()
        {
            if (AppSettings.EnableDocking)
            {
                ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), GetDockSpaceFlags());
            }

            if (SubmitOverrideUI())
            {
                settingsDialog_?.Build();
                BuildAppModalDialogs();
                UndoEditTracker.EndFrame();
                return;
            }

            BuildImGuiDemoWindow();

            // created lazily, so the derived class is fully constructed before CreateMainMenu() is called
            if (mainMenu_ == null)
                mainMenu_ = CreateMainMenu();
            mainMenu_.Build();

            foreach(UiWindowBase wnd in allWindows_)
                wnd.Build();

            // modal - after the windows
            settingsDialog_?.Build();
            BuildAppModalDialogs();

            // an edited widget which was not submitted in this frame (e.g. selection changed) saves its undo action
            UndoEditTracker.EndFrame();
        }

        private void BuildImGuiDemoWindow()
        {
            // Demo code adapted from the official Dear ImGui demo program:
            // https://github.com/ocornut/imgui/blob/master/examples/example_win32_directx11/main.cpp#L172
            // https://github.com/ocornut/imgui/blob/master/imgui_demo.cpp
            if (ShowImGuiDemoWindow)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref ShowImGuiDemoWindow);
            }
        }

        public void ApplyLayout(LayoutData layoutData)
        {
            foreach(var keyVal in layoutData.WindowsVisibility)
            {
                UiWindowBase? wnd = FindWindowByName(keyVal.Key);
                if (wnd == null)
                    continue;

                wnd.IsVisible = keyVal.Value;
            }
        }
    }
}
