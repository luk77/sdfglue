using ImGuiNET;
using SingleDocAppCore.UndoSystem;
using SingleDocAppFramework.Ui;
using SingleDocAppFramework.Ui.Properties;

namespace SingleDocAppSample.Windows
{
    public class WndStats : UiWindowBase
    {
        public override string Title => "Statistics";

        private ISampleExecutor SampleExecutor { get { return (ISampleExecutor)Executor; } }

        public override void Build()
        {
            BuildWindow(uiMgr_.RightColPosX, uiMgr_.BasePosY, uiMgr_.RightColWidth, uiMgr_.BaseHeight / 3, delegate()
            {
                string text = SampleExecutor.GetDocument().GetText();

                int lines = text.Length == 0 ? 0 : text.Count(c => c == '\n') + 1;
                int words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

                int id = 0;
                BeginPropertyGrid(GetDefaultFirstColumnWidth() * 0.5f);
                UiString.BuildReadonly(ref id, "Characters" , text.Length.ToString());
                UiString.BuildReadonly(ref id, "Words"      , words.ToString());
                UiString.BuildReadonly(ref id, "Lines"      , lines.ToString());
                EndPropertyGrid();

                ImGui.Spacing();
                ImGui.TextDisabled(String.Format("Undo: {0}, Redo: {1}", UndoManager.Instance.CanUndo, UndoManager.Instance.CanRedo));
            });
        }
    }
}
