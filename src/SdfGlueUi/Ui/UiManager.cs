//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueUi.Ui.Components;
using SdfGlueUi.Ui.Windows;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.Model.DataNodes;
using SdfGlueUi.Layouts;

namespace SdfGlueUi.Ui
{
    public class UiManager
    {
        public  IUiActionsExecutor      ActionsExecutor         = null;
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

        public  int                     ExplorerHeight          = 0;
        public  int                     InspectorPosY           = 0;
        public  int                     InspectorHeight         = 0;
        public  int                     PlaybackHeight          = 0;
        public  int                     PlaybackPosY            = 0;
        public  int                     PreviewWidth            = 0;
        public  int                     PreviewHeight           = 0;
        public  int                     MaterialsHeight         = 0;
        public  int                     RenderParamsPosY        = 0;
        public  int                     RenderParamsHeight      = 0;
        public  int                     LogPosY                 = 0;
        public  int                     LogHeight               = 0;

        public  bool                    EnabledDemoMode         = false;

        private bool                    fullPreviewMode_        = false;
        private bool                    showDemoWindow_         = false;
        public  bool                    AutoLayoutWindows       = false;//true;
        private List<UiWindowBase>      allWindows_             = new List<UiWindowBase>();
        private List<UiWindowBase>      previewWindows_         = new List<UiWindowBase>();
        private List<UiWindowBase>      codeWindows_            = new List<UiWindowBase>();
        private List<UiWindowBase>      otherWindows_           = new List<UiWindowBase>();

        private UiWindowBase            WindowPreview           = null;
        private UiWindowBase            WindowExplorer          = null;
        private UiWindowBase            WindowInspector         = null;
        private UiWindowBase            WindowGeneratedCode     = null;
        private UiWindowBase            WindowLog               = null;
        private UiWindowBase            WindowSettings          = null;
        private UiWindowBase            WindowDiagnostics       = null;
        private UiWindowBase            WindowPlayback          = null;

        //private List<WndPreview>        AdditionalPreviews      = new List<WndPreview>();
        public UiManager(IUiActionsExecutor uiActionsExecutor, int mainWindowSizeX, int mainWindowSizeY)
        {
            ActionsExecutor = uiActionsExecutor;

            if (DataModel.UseMultiplePreviews)
            {
                for(int i=0; i<DataModel.NumOfPreviews; i++)
                {
                    WndPreview? wndPreview = RegisterWindow(new WndPreview(String.Format("Preview {0}", i+1)), previewWindows_, i == 0) as WndPreview;
                    //AdditionalPreviews.Add(wndPreview);

                    if (i == 0)
                        WindowPreview = wndPreview;
                }
            }
            else
            {
                WindowPreview           = RegisterWindow(new WndPreview("Preview"), otherWindows_);
            }
            WindowExplorer          = RegisterWindow(new WndExplorer()            , otherWindows_);
            WindowInspector         = RegisterWindow(new WndInspector()           , otherWindows_);
            if (DataModel.UseMultipleCodeViews)
            {
                for(int i=0; i<DataModel.NumOfCodeViews; i++)
                {
                    WndGeneratedCode? wnd = RegisterWindow(new WndGeneratedCode(String.Format("Code view {0}", i+1)), codeWindows_, false) as WndGeneratedCode;
                    if (i == 0)
                        WindowGeneratedCode = wnd;
                }
            }
            else
            {
                WindowGeneratedCode     = RegisterWindow(new WndGeneratedCode("Generated Code")       , otherWindows_, false);
            }
            WindowLog               = RegisterWindow(new WndLog()                 , otherWindows_, false);
            WindowSettings          = RegisterWindow(new WndSettings()            , otherWindows_, false);
            WindowDiagnostics       = RegisterWindow(new WndDiagnostics()         , otherWindows_, false);
            WindowPlayback          = RegisterWindow(new WndPlayback()            , otherWindows_);


            SetMainWindowClientSize(mainWindowSizeX, mainWindowSizeY);
        }

        public void SetMainWindowClientSize(int mainWindowSizeX, int mainWindowSizeY)
        {
            //bool previewExpandWidth = !(WindowMaterials.IsVisible || WindowSceneInspector.IsVisible);
            bool previewExpandWidth = true;//!(WindowMaterials.IsVisible);
            //bool previewExpandHeight = !(WindowRendererParams.IsVisible || WindowLog.IsVisible || WindowDiagnostics.IsVisible);
            bool previewExpandHeight = !(WindowLog.IsVisible || WindowDiagnostics.IsVisible);

            //WindowsScaling      = UiManager.GetWindowsScaling();
            WindowsScaling      = ActionsExecutor.GetWindowsScaling();

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

            ExplorerHeight      = (int)(0.35 * MainWindowSizeY);
            PlaybackHeight      = (int)(0.16 * MainWindowSizeY);
            InspectorPosY       = BasePosY + ExplorerHeight + DistanceY;
            InspectorHeight     = MainWindowSizeY - ExplorerHeight - PlaybackHeight - 4 * DistanceY - MenuHeight;
            PlaybackPosY        = InspectorPosY + InspectorHeight + DistanceY;
            LogPosY             = PlaybackPosY;
            LogHeight           = PlaybackHeight;
            PreviewWidth        = previewExpandWidth ? (CenterColWidth + RightColWidth + DistanceX) : CenterColWidth;
            PreviewHeight       = previewExpandHeight ? BaseHeight : (int)(BaseHeight - PlaybackHeight - DistanceY);
            MaterialsHeight     = (int)(MainWindowSizeY * 0.5);
            RenderParamsPosY    = BasePosY + PreviewHeight + DistanceY;
            RenderParamsHeight  = 0;//(WindowLog.IsVisible || WindowDiagnostics.IsVisible) ? ((int)(MainWindowSizeY * 0.2)) : (MainWindowSizeY - PreviewHeight - 3 * DistanceY - MenuHeight);
            //LogPosY             = RenderParamsPosY + RenderParamsHeight + DistanceY;
            //LogHeight           = MainWindowSizeY - PreviewHeight - RenderParamsHeight - 4 * DistanceY - MenuHeight;
        }

        public void SetFullPreviewMode(bool fullPreviewMode)
        {
            fullPreviewMode_ = fullPreviewMode;
        }
        public bool GetFullPreviewMode()
        {
            return fullPreviewMode_;
        }

        private UiWindowBase RegisterWindow(UiWindowBase window, List<UiWindowBase> helperList, bool visibleOnStart = true)
        {
            window.IsVisible = visibleOnStart;
            window.SetUiManager(this);
            allWindows_.Add(window);

            helperList.Add(window);

            return window;
        }

        public void SubmitUI()
        {
            if (DataModel.UseDocking)
            {
                ImGui.DockSpaceOverViewport();
                //BuildDockSpace();
            }

            if (fullPreviewMode_)
            {
                if (WindowPreview != null)
                    ((WndPreview)WindowPreview).BuildFullPreview();
            }
            else
            {
                BuildWindowImGuiDemo();

                BuildMainMenu();

                foreach(UiWindowBase wnd in allWindows_)
                    wnd.Build();
                // Debug
                //for(int i=0; i<allWindows_.Count; i++)
                //{
                //    UiWindowBase wnd = allWindows_[i];
                //    wnd.Build();
                //}
            }
        }

        public void HandleInput()
        {
            foreach(UiWindowBase wnd in allWindows_)
            {
                wnd.HandleInput();
            }
        }

        private void BuildWindowImGuiDemo()
        {
            // Demo code adapted from the official Dear ImGui demo program:
            // https://github.com/ocornut/imgui/blob/master/examples/example_win32_directx11/main.cpp#L172
            // https://github.com/ocornut/imgui/blob/master/imgui_demo.cpp
            if (showDemoWindow_)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(650, 20), ImGuiCond.FirstUseEver);
                ImGui.ShowDemoWindow(ref showDemoWindow_);
            }
        }

        private void BuildMainMenu()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    bool hasFileOpened = !string.IsNullOrEmpty(ActionsExecutor.GetModel().ProjectFilePath);
                    if (ImGui.MenuItem("New"        , "CTRL+N"))                        { ActionsExecutor.OnNewProject();       }
                    if (ImGui.MenuItem("Open"       , "CTRL+O"))                        { ActionsExecutor.OnOpenProject();      }
                    if (ImGui.MenuItem("Save"       , "CTRL+S", false, hasFileOpened))  { ActionsExecutor.OnSaveProject();      }
                    if (ImGui.MenuItem("Save as"    , "CTRL+SHIFT+S"))                  { ActionsExecutor.OnSaveProjectAs();    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Save image as", "CTRL+I"))                      { ActionsExecutor.OnSaveImage(); }
                    if (ImGui.MenuItem("Export image sequence", ""))                    { ActionsExecutor.OnExportAnimation(); }
                    ImGui.Separator();
                    if (ImGui.BeginMenu("Examples"))
                    {
                        string exampleToOpen = MenuFileTree.Build("Examples", "*.xml");
                        if (!String.IsNullOrEmpty(exampleToOpen))
                        {
                            ActionsExecutor.OnOpenProject(exampleToOpen);
                        }
                        ImGui.EndMenu();
                    }

                    ImGui.Separator();
                    if (ImGui.MenuItem("Exit"       , "Alt+F4"))                        { ActionsExecutor.OnExitApp();    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Edit"))
                {
                    if (ImGui.MenuItem("Undo", "CTRL+Z", false, true)) { UndoManager.Instance.DoUndo(); }
                    if (ImGui.MenuItem("Redo", "CTRL+Y", false, true)) { UndoManager.Instance.DoRedo(); }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Cut", "CTRL+X"))    { ActionsExecutor.OnCutObject   (ActionsExecutor.GetModel().SelectedNode as SdfObject); }
                    if (ImGui.MenuItem("Copy", "CTRL+C"))   { ActionsExecutor.OnCopyObject  (ActionsExecutor.GetModel().SelectedNode as SdfObject); }
                    if (ImGui.MenuItem("Paste", "CTRL+V"))  { ActionsExecutor.OnPasteObject (ActionsExecutor.GetModel().SelectedNode as SdfObject); }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("Tools"))
                {
                    if (ImGui.MenuItem("Reload SDF Definitions", "CTRL+E"))         { ActionsExecutor.OnReloadSdfDefinitions(); }
                    if (ImGui.MenuItem("Compile shader", "CTRL+R, CTRL+ENTER"))     { ActionsExecutor.OnRebuildShader(); }
                    if (ImGui.MenuItem("Reset frame counter", ""))                  { ActionsExecutor.OnResetFrameCounter(); }
                    if (ImGui.MenuItem("Batch process all projects", ""))           { ActionsExecutor.OnBatchProcessAllProjects(); }
                    
                    ImGui.Separator();
                    ImGui.Checkbox("Demo mode", ref EnabledDemoMode);

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu("View"))
                {
                    foreach(UiWindowBase wnd in otherWindows_)
                    {
                        ImGui.Checkbox(wnd.Title, ref wnd.IsVisible);
                    }

                    ImGui.Separator();

                    if (ImGui.BeginMenu("Previews"))
                    {
                        foreach(UiWindowBase wnd in previewWindows_)
                        {
                            ImGui.Checkbox(wnd.Title, ref wnd.IsVisible);
                        }
                        ImGui.EndMenu();
                    }

                    ImGui.Separator();

                    if (ImGui.BeginMenu("Code"))
                    {
                        foreach(UiWindowBase wnd in codeWindows_)
                        {
                            ImGui.Checkbox(wnd.Title, ref wnd.IsVisible);
                        }
                        ImGui.EndMenu();
                    }

                    ImGui.Separator();
                    ImGui.Checkbox("Full screen preview"  , ref fullPreviewMode_  );

                    ImGui.Checkbox("Auto layout windows", ref AutoLayoutWindows);

                    ImGui.Separator();
                    ImGui.Checkbox("ImGui demo"         , ref showDemoWindow_   );

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Layouts"))
                {
                    if (ImGui.BeginMenu("Load layout"))
                    {
                        //string layoutToOpen = MenuFileTree.Build("Layouts", "*.ini");
                        string layoutToOpen = MenuFileTree.Build("Layouts", "*.xml");
                        if (!String.IsNullOrEmpty(layoutToOpen))
                        {
                            ActionsExecutor.OnLoadLayout(layoutToOpen);
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.MenuItem("Save current layout", ""))
                    {
                        WindowsVisibilityCollection windowsVisibility = new WindowsVisibilityCollection();
                        foreach(UiWindowBase wnd in allWindows_)
                            windowsVisibility.Add(wnd.Title, wnd.IsVisible);

                        ActionsExecutor.OnSaveCurrentLayout(windowsVisibility);
                    }

                    ImGui.EndMenu();
                }
//                if (ImGui.BeginMenu("Debug"))
//                {
//                    if (ImGui.MenuItem("Upgrade project", ""))
//                    {
//                        ActionsExecutor.GetModel().UpgradeProject();
//                        ActionsExecutor.OnRebuildShader();
//                    }
//                    ImGui.EndMenu();
//                }
                ImGui.EndMainMenuBar();
            }
        }

        public void ToggleWindow(int windowIndex)
        {
            if (windowIndex < 0)
                return;

            if (windowIndex >= allWindows_.Count)
                return;

            allWindows_[windowIndex].IsVisible = !allWindows_[windowIndex].IsVisible;
        }

        public UiWindowBase FindFocusedWindow()
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
            UiWindowBase wnd = FindFocusedWindow();
            if (wnd == null)
                return;

            wnd.IsVisible = false;
        }

        // Variables to configure the Dockspace example.
        private bool                _dockspaceOpen          = true;
        private bool                _dockspaceFullscreen    = true; // Is the Dockspace full-screen?
        private bool                _dockspaceUsePadding    = false; // Is there padding (a blank space) between the window edge and the Dockspace?
        private ImGuiDockNodeFlags  _dockspaceFlags         = ImGuiDockNodeFlags.None; // Config flags for the Dockspace

        void BuildDockSpace()
        {
            // In this example, we're embedding the Dockspace into an invisible parent window to make it more configurable.
            // We set ImGuiWindowFlags_NoDocking to make sure the parent isn't dockable into because this is handled by the Dockspace.
            //
            // ImGuiWindowFlags_MenuBar is to show a menu bar with config options. This isn't necessary to the functionality of a
            // Dockspace, but it is here to provide a way to change the configuration flags interactively.
            // You can remove the MenuBar flag if you don't want it in your app, but also remember to remove the code which actually
            // renders the menu bar, found at the end of this function.
            ImGuiWindowFlags window_flags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;

            // Is the example in Fullscreen mode?
            if (_dockspaceFullscreen)
            {
                // If so, get the main viewport:
                ImGuiViewportPtr viewport = ImGui.GetMainViewport();

                // Set the parent window's position, size, and viewport to match that of the main viewport. This is so the parent window
                // completely covers the main viewport, giving it a "full-screen" feel.
                ImGui.SetNextWindowPos(viewport.WorkPos);
                ImGui.SetNextWindowSize(viewport.WorkSize);
                ImGui.SetNextWindowViewport(viewport.ID);

                // Set the parent window's styles to match that of the main viewport:
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f); // No corner rounding on the window
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f); // No border around the window

                // Manipulate the window flags to make it inaccessible to the user (no titlebar, resize/move, or navigation)
                window_flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
                window_flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;
            }
            else
            {
                // The example is not in Fullscreen mode (the parent window can be dragged around and resized), disable the
                // ImGuiDockNodeFlags_PassthruCentralNode flag.
                _dockspaceFlags &= ~ImGuiDockNodeFlags.PassthruCentralNode;
            }

            // When using ImGuiDockNodeFlags_PassthruCentralNode, DockSpace() will render our background
            // and handle the pass-thru hole, so the parent window should not have its own background:
            if ((int)(_dockspaceFlags & ImGuiDockNodeFlags.PassthruCentralNode) != 0)
                window_flags |= ImGuiWindowFlags.NoBackground;

            // If the padding option is disabled, set the parent window's padding size to 0 to effectively hide said padding.
            if (!_dockspaceUsePadding)
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));

            // Important: note that we proceed even if Begin() returns false (aka window is collapsed).
            // This is because we want to keep our DockSpace() active. If a DockSpace() is inactive,
            // all active windows docked into it will lose their parent and become undocked.
            // We cannot preserve the docking relationship between an active window and an inactive docking, otherwise
            // any change of dockspace/settings would lead to windows being stuck in limbo and never being visible.
            ImGui.Begin("DockSpace Demo", ref _dockspaceOpen, window_flags);

            // Remove the padding configuration - we pushed it, now we pop it:
            if (!_dockspaceUsePadding)
                ImGui.PopStyleVar();

            // Pop the two style rules set in Fullscreen mode - the corner rounding and the border size.
            if (_dockspaceFullscreen)
                ImGui.PopStyleVar(2);

            // Check if Docking is enabled:
            ImGuiIOPtr io = ImGui.GetIO();
            if ((int)(io.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0)
            {
                // If it is, draw the Dockspace with the DockSpace() function.
                // The GetID() function is to give a unique identifier to the Dockspace - here, it's "MyDockSpace".
                uint dockspace_id = ImGui.GetID("MyDockSpace");
                ImGui.DockSpace(dockspace_id, new System.Numerics.Vector2(0.0f, 0.0f), _dockspaceFlags);
            }
            else
            {
                // Docking is DISABLED - Show a warning message
                //ShowDockingDisabledMessage();
            }

            // This is to show the menu bar that will change the config settings at runtime.
            // If you copied this demo function into your own code and removed ImGuiWindowFlags_MenuBar at the top of the function,
            // you should remove the below if-statement as well.
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("Options"))
                {
                    // Disabling fullscreen would allow the window to be moved to the front of other windows,
                    // which we can't undo at the moment without finer window depth/z control.
                    ImGui.MenuItem("Fullscreen", null, ref _dockspaceFullscreen);
                    ImGui.MenuItem("Padding", null, ref _dockspaceUsePadding);
                    ImGui.Separator();

                    // Display a menu item for each Dockspace flag, clicking on one will toggle its assigned flag.
                    if (ImGui.MenuItem("Flag: NoSplit",                "", (_dockspaceFlags & ImGuiDockNodeFlags.NoDockingSplit) != 0))                        { _dockspaceFlags ^= ImGuiDockNodeFlags.NoDockingSplit; }
                    if (ImGui.MenuItem("Flag: NoResize",               "", (_dockspaceFlags & ImGuiDockNodeFlags.NoResize) != 0))                              { _dockspaceFlags ^= ImGuiDockNodeFlags.NoResize; }
                    if (ImGui.MenuItem("Flag: NoDockingInCentralNode", "", (_dockspaceFlags & ImGuiDockNodeFlags.NoDockingOverCentralNode) != 0))              { _dockspaceFlags ^= ImGuiDockNodeFlags.NoDockingOverCentralNode; }
                    if (ImGui.MenuItem("Flag: AutoHideTabBar",         "", (_dockspaceFlags & ImGuiDockNodeFlags.AutoHideTabBar) != 0))                        { _dockspaceFlags ^= ImGuiDockNodeFlags.AutoHideTabBar; }
                    if (ImGui.MenuItem("Flag: PassthruCentralNode",    "", (_dockspaceFlags & ImGuiDockNodeFlags.PassthruCentralNode) != 0, _dockspaceFullscreen))   { _dockspaceFlags ^= ImGuiDockNodeFlags.PassthruCentralNode; }
                    ImGui.Separator();

                    // Display a menu item to close this example.
                    //if (ImGui.MenuItem("Close", null, false, _dockspaceOpen != null))
                    //    if (_dockspaceOpen != null) // Remove MSVC warning C6011 (NULL dereference) - the `_dockspaceOpen != NULL` in MenuItem() does prevent NULL derefs, but IntelliSense doesn't analyze that deep so we need to add this in ourselves.
                    //        *_dockspaceOpen = false; // Changing this variable to false will close the parent window, therefore closing the Dockspace as well.
                    if (ImGui.MenuItem("Close", null, false, _dockspaceOpen))
                        if (_dockspaceOpen) // Remove MSVC warning C6011 (NULL dereference) - the `_dockspaceOpen != NULL` in MenuItem() does prevent NULL derefs, but IntelliSense doesn't analyze that deep so we need to add this in ourselves.
                            _dockspaceOpen = false; // Changing this variable to false will close the parent window, therefore closing the Dockspace as well.
                    ImGui.EndMenu();
                }

                // Show a help marker that gives an overview of what this example is and does.
                //HelpMarker(
                //    "When docking is enabled, you can ALWAYS dock MOST window into another! Try it now!" "\n"
                //    "- Drag from window title bar or their tab to dock/undock." "\n"
                //    "- Drag from window menu button (upper-left button) to undock an entire node (all windows)." "\n"
                //    "- Hold SHIFT to disable docking." "\n"
                //    "This demo app has nothing to do with it!" "\n\n"
                //    "This demo app only demonstrates the use of ImGui::DockSpace() which allows you to manually create a docking node _within_ another window." "\n\n"
                //    "Read comments in ShowExampleAppDockSpace() for more details.");

                ImGui.EndMenuBar();
            }

            // End the parent window that contains the Dockspace:
            ImGui.End();
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

        private UiWindowBase? FindWindowByName(string wndName)
        {
            foreach(UiWindowBase wnd in allWindows_)
            {
                if (wndName == wnd.Title)
                    return wnd;
            }

            return null;
        }
    }
}
