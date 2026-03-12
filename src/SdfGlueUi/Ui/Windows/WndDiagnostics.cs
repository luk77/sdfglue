//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using System.Globalization;

namespace SdfGlueUi.Ui.Windows
{
    public class WndDiagnostics : UiWindowBase
    {
        public override string Title => "Diagnostics";

        public override void Build()
        {
            BuildWindow(uiMgr_.CenterColPosX, uiMgr_.LogPosY, uiMgr_.CenterColWidth, uiMgr_.LogHeight, delegate()
            {
                if (ImGui.CollapsingHeader("Rendering", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Checkbox("Enable shader rendering", ref GetModel().Config.EnableRendering);
                }

                if (ImGui.CollapsingHeader("Mouse", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    /*
                    MouseState mouseState = Mouse.GetCursorState();

                    ImGui.Text(String.Format("X: {0}", mouseState.X));
                    ImGui.Text(String.Format("Y: {0}", mouseState.Y));
                    ImGui.Text(String.Format("Scroll X: {0}", mouseState.Scroll.X.ToString("0.000", CultureInfo.InvariantCulture)));
                    ImGui.Text(String.Format("Scroll Y: {0}", mouseState.Scroll.Y.ToString("0.000", CultureInfo.InvariantCulture)));
                    ImGui.Text(String.Format("WheelPrecise: {0}", mouseState.WheelPrecise.ToString("0.0", CultureInfo.InvariantCulture)));
                    */

                    ImGui.Text(String.Format("X: {0}", uiMgr_.ActionsExecutor.GetMouseStateX()));
                    ImGui.Text(String.Format("Y: {0}", uiMgr_.ActionsExecutor.GetMouseStateY()));
                    ImGui.Text(String.Format("WheelPrecise: {0}", uiMgr_.ActionsExecutor.GetWheelPrecise().ToString("0.0", CultureInfo.InvariantCulture)));
                }

            });
        }
    }
}
