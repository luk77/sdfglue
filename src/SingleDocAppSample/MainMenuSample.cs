using ImGuiNET;
using SingleDocAppFramework.Ui.Menu;

namespace SingleDocAppSample
{
    // Adds the "Text" menu between "Edit" and "View"
    public class MainMenuSample : MainMenuBase
    {
        private UiManagerSample         uiMgrSample_;

        public MainMenuSample(UiManagerSample uiMgr)
            : base(uiMgr)
        {
            uiMgrSample_ = uiMgr;
        }

        protected override void BuildAppMenus()
        {
            if (ImGui.BeginMenu("Text"))
            {
                if (ImGui.MenuItem("To upper case"))    { uiMgrSample_.SampleExecutor.OnToUpperCase(); }
                if (ImGui.MenuItem("To lower case"))    { uiMgrSample_.SampleExecutor.OnToLowerCase(); }
                ImGui.EndMenu();
            }
        }
    }
}
