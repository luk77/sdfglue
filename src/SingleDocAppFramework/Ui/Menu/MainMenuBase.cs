using ImGuiNET;
using SingleDocAppCore.UndoSystem;
using SingleDocAppFramework.Ui.Components;

namespace SingleDocAppFramework.Ui.Menu
{
    // Main menu bar with the menus common to all applications:
    //   File    - New, Open, Save, Save as, [BuildFileMenuExtras], Exit
    //   Edit    - Undo, Redo, [BuildEditMenuExtras]
    //   [BuildAppMenus]  - application-specific menus (e.g. Tools)
    //   View    - windows visibility (grouped by view group), [BuildViewMenuExtras], ImGui demo
    //   Layouts - Load layout, Save current layout
    // The application derives from this class and overrides the Build*Extras / BuildAppMenus hooks.
    public class MainMenuBase
    {
        protected   UiManagerBase           uiMgr_;

        public MainMenuBase(UiManagerBase uiMgr)
        {
            uiMgr_ = uiMgr;
        }

        protected IUiExecutorFramework Executor { get { return uiMgr_.Executor; } }

        public void Build()
        {
            if (!ImGui.BeginMainMenuBar())
                return;

            if (ImGui.BeginMenu("File"))
            {
                BuildFileMenu();
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Edit"))
            {
                BuildEditMenu();
                ImGui.EndMenu();
            }

            BuildAppMenus();

            if (ImGui.BeginMenu("View"))
            {
                BuildViewMenu();
                ImGui.EndMenu();
            }
            if (ImGui.BeginMenu("Layouts"))
            {
                BuildLayoutsMenu();
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        // File
        protected virtual void BuildFileMenu()
        {
            if (ImGui.MenuItem("New"        , "CTRL+N"))                                        { Executor.OnNewDocument();     }
            if (ImGui.MenuItem("Open"       , "CTRL+O"))                                        { Executor.OnOpenDocument();    }
            if (ImGui.MenuItem("Save"       , "CTRL+S", false, Executor.CanSaveDocument()))     { Executor.OnSaveDocument();    }
            if (ImGui.MenuItem("Save as"    , "CTRL+SHIFT+S"))                                  { Executor.OnSaveDocumentAs();  }

            BuildFileMenuExtras();

            ImGui.Separator();
            if (ImGui.MenuItem("Exit"       , "Alt+F4"))                                        { Executor.OnExitApp();         }
        }

        // Items between "Save as" and "Exit" (add a leading separator if needed)
        protected virtual void BuildFileMenuExtras()
        {
        }

        // Edit
        protected virtual void BuildEditMenu()
        {
            if (ImGui.MenuItem("Undo", "CTRL+Z", false, UndoManager.Instance.CanUndo))  { Executor.OnUndo(); }
            if (ImGui.MenuItem("Redo", "CTRL+Y", false, UndoManager.Instance.CanRedo))  { Executor.OnRedo(); }

            BuildEditMenuExtras();
        }

        // Items after "Redo" (add a leading separator if needed)
        protected virtual void BuildEditMenuExtras()
        {
        }

        // Application-specific top-level menus, placed between "Edit" and "View"
        protected virtual void BuildAppMenus()
        {
        }

        // View
        protected virtual void BuildViewMenu()
        {
            // windows without a group
            foreach(UiWindowBase wnd in uiMgr_.GetWindowsInViewGroup(null))
            {
                ImGui.Checkbox(wnd.Title, ref wnd.IsVisible);
            }

            // grouped windows as submenus
            foreach(string group in uiMgr_.GetViewGroups())
            {
                ImGui.Separator();

                if (ImGui.BeginMenu(group))
                {
                    foreach(UiWindowBase wnd in uiMgr_.GetWindowsInViewGroup(group))
                    {
                        ImGui.Checkbox(wnd.Title, ref wnd.IsVisible);
                    }
                    ImGui.EndMenu();
                }
            }

            ImGui.Separator();
            BuildViewMenuExtras();

            ImGui.Separator();
            ImGui.Checkbox("ImGui demo", ref uiMgr_.ShowImGuiDemoWindow);
        }

        // Items after the windows list, before "ImGui demo" (followed by a separator)
        protected virtual void BuildViewMenuExtras()
        {
        }

        // Layouts
        protected virtual void BuildLayoutsMenu()
        {
            SdAppSettings settings = uiMgr_.AppSettings;

            if (ImGui.BeginMenu("Load layout"))
            {
                string layoutToOpen = MenuFileTree.Build(settings.LayoutsDirectory, settings.LayoutFileFilter.Pattern);
                if (!String.IsNullOrEmpty(layoutToOpen))
                {
                    Executor.OnLoadLayout(layoutToOpen);
                }
                ImGui.EndMenu();
            }
            if (ImGui.MenuItem("Save current layout", ""))
            {
                Executor.OnSaveCurrentLayout(uiMgr_.GetWindowsVisibility());
            }
        }
    }
}
