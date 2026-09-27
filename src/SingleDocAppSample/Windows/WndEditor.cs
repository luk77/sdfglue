using ImGuiNET;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using SingleDocAppFramework.Ui;
using System.Numerics;

namespace SingleDocAppSample.Windows
{
    public class WndEditor : UiWindowBase
    {
        private static readonly uint    MaxTextLength           = 1024 * 1024;

        public override string Title => "Editor";

        private ISampleExecutor SampleExecutor { get { return (ISampleExecutor)Executor; } }

        public override void Build()
        {
            BuildWindow(uiMgr_.LeftColPosX, uiMgr_.BasePosY, uiMgr_.LeftColWidth + uiMgr_.CenterColWidth + uiMgr_.DistanceX, uiMgr_.BaseHeight, delegate()
            {
                TextDocument document = SampleExecutor.GetDocument();

                string text = document.GetText();
                ImGui.InputTextMultiline("##text", ref text, MaxTextLength, new Vector2(-1.0f, -1.0f));
                document.Text.Val = text;

                // One undo step per editing session (ImGui handles undo while the field is active)
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    UndoManager.Instance.SaveAction(new ActionString(document.Text));
                }
            });
        }
    }
}
