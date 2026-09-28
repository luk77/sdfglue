using ImGuiNET;
using System.Numerics;

namespace SingleDocAppFramework.Ui
{
    public abstract class UiWindowBase
    {
        public      bool                    IsVisible               = true;
        public      bool                    IsFocused               = false;
        public      bool                    IsHovered               = false;
        public      string?                 ViewGroup               = null;     // "View" submenu, set by UiManagerBase.RegisterWindow()
        protected   UiManagerBase           uiMgr_                  = null!;    // set by UiManagerBase.RegisterWindow()

        protected delegate void BuildContent();

        //public delegate void OnValueChanged();
        public abstract string Title { get; }


        public void SetUiManager(UiManagerBase uiMgr)
        {
            uiMgr_ = uiMgr;
        }

        protected IUiExecutorFramework Executor { get { return uiMgr_.Executor; } }

        // Width of the first (label) column in property grids, adjusted to the UI scaling
        public float GetDefaultFirstColumnWidth()
        {
            float bonus = Executor.GetWindowsScaling() - 1.0f;

            return 220.0f + bonus * 120.0f;
        }

        // Window position and size come from the layout (imgui ini settings) and docking
        protected void BuildWindow(BuildContent buildContent)
        {
            BuildWindow(ImGuiWindowFlags.None, buildContent);
        }

        protected void BuildWindow(ImGuiWindowFlags flags, BuildContent buildContent)
        {
            if (!IsVisible)
                return;

            IsFocused = false;

            ImGui.SetNextWindowSizeConstraints(new Vector2(340.0f, 100.0f), new Vector2(float.MaxValue, float.MaxValue));

            if (!ImGui.Begin(Title, ref IsVisible, flags))
            {
                ImGui.End();
                return;
            }

            IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootWindow);
            IsHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootWindow);

            // debug
            //ImGui.Text(string.Format("IsFocused: {0}", IsFocused));
            //ImGui.Text(string.Format("IsHovered: {0}", IsHovered));

            buildContent();

            ImGui.End();
        }

        public static void BeginPropertyGrid(float firstColumnWidth)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2,2));
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, firstColumnWidth);
            ImGui.Separator();
        }

        public static void EndPropertyGrid()
        {
            ImGui.Columns(1);
            ImGui.Separator();
            ImGui.PopStyleVar();
        }

        public abstract void Build();

        public virtual void HandleInput(float deltaTime)
        {
        }

        // Helper to display a little (?) mark which shows a tooltip when hovered.
        // In your own code you may want to display an actual icon if you are using a merged icon fonts (see docs/FONTS.txt)
        public static void HelpMarker(string desc)
        {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted(desc);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

    }
}
