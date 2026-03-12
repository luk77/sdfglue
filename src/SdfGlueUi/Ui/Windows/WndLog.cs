//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;

namespace SdfGlueUi.Ui.Windows
{
    public class WndLog : UiWindowBase
    {
        public override string Title => "Log";

        public override void Build()
        {
            BuildWindow(uiMgr_.CenterColPosX, uiMgr_.LogPosY, uiMgr_.CenterColWidth, uiMgr_.LogHeight, delegate()
            {
                if (ImGui.Button("Clear"))
                {
                    DataModel.OutputLog.Clear();
                }

                // Wygląda na to że kontrolka ImGui ma jakiś limit na ilość tekstu...
                //ImGui.Text(DataModel.OutputLog.ToString());
                string log = DataModel.OutputLog.ToString();
                int lengthToShow = 2048;
                if (lengthToShow > log.Length)
                    lengthToShow = log.Length;
                ImGui.Text(log.Substring(log.Length - lengthToShow, lengthToShow));
            });
        }
    }
}
