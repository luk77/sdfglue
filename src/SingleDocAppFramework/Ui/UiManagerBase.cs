using ImGuiNET;
using SingleDocAppFramework.Layouts;
using SingleDocAppFramework.Ui.Menu;

namespace SingleDocAppFramework.Ui
{
    public abstract class UiManagerBase
    {
        public  float                   WindowsScaling          = 1.0f;
        public  int                     MainWindowSizeX         = 0;
        public  int                     MainWindowSizeY         = 0;
        public  int                     MenuHeight              = 0;
        public  int                     DistanceX               = 0;
        public  int                     DistanceY               = 0;
        public  int                     BasePosY                = 0;
        public  int                     BaseHeight              = 0;
        public  int                     LeftColWidth            = 0;
        public  int                     RightColWidth           = 0;
        public  int                     CenterColWidth          = 0;
        public  int                     LeftColPosX             = 0;
        public  int                     CenterColPosX           = 0;
        public  int                     RightColPosX            = 0;

        public  bool                    AutoLayoutWindows       = false;//true;
        public  bool                    ShowImGuiDemoWindow     = false;

        public  SdAppSettings           AppSettings             = new SdAppSettings();    // set by SdAppWindow

        private IUiExecutorFramework    executor_;
        private MainMenuBase?           mainMenu_               = null;

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

        public void HandleInput(float deltaTime)
        {
            foreach(UiWindowBase wnd in allWindows_)
            {
                wnd.HandleInput(deltaTime);
            }
        }

        public virtual void SetMainWindowClientSize(int mainWindowSizeX, int mainWindowSizeY)
        {
            MainWindowSizeX     = mainWindowSizeX;
            MainWindowSizeY     = mainWindowSizeY;

            MenuHeight          = (int)(12.0f * WindowsScaling);//(0.02 * MainWindowSizeY * );
            DistanceX           = (int)(0.005 * MainWindowSizeX);
            DistanceY           = DistanceX;
            BasePosY            = MenuHeight + DistanceY;
            BaseHeight          = MainWindowSizeY - MenuHeight - 2 * DistanceY;

            LeftColWidth        = (int)(0.25 * MainWindowSizeX);
            RightColWidth       = (int)(0.25 * MainWindowSizeX);
            CenterColWidth      = MainWindowSizeX - LeftColWidth - RightColWidth - 4 * DistanceX;

            LeftColPosX         = DistanceX;
            CenterColPosX       = LeftColWidth + 2 * DistanceX;
            RightColPosX        = MainWindowSizeX - RightColWidth - DistanceX;
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

        public void SubmitUI()
        {
            if (AppSettings.EnableDocking)
            {
                ImGui.DockSpaceOverViewport();
            }

            if (SubmitOverrideUI())
                return;

            BuildImGuiDemoWindow();

            // created lazily, so the derived class is fully constructed before CreateMainMenu() is called
            if (mainMenu_ == null)
                mainMenu_ = CreateMainMenu();
            mainMenu_.Build();

            foreach(UiWindowBase wnd in allWindows_)
                wnd.Build();
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
