//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppFramework.Diagnostics;

namespace SingleDocAppFramework.Ui.Windows
{
    // Displays the tail of AppLog
    public class WndLog : UiWindowBase
    {
        private static readonly int     MaxDisplayedLength      = 2048;

        // Optional initial placement (used on first use or with "Auto layout windows");
        // by default the window is placed at the bottom of the center column
        public  Func<WindowRect>?       PlacementProvider       = null;

        public override string Title => "Log";

        public override void Build()
        {
            WindowRect rect = PlacementProvider != null ? PlacementProvider() : GetDefaultPlacement();

            BuildWindow(rect.PosX, rect.PosY, rect.SizeX, rect.SizeY, delegate()
            {
                if (ImGui.Button("Clear"))
                {
                    AppLog.Clear();
                }

                // It looks like the ImGui text control has a limit on the amount of text...
                string log = AppLog.GetText();
                int lengthToShow = Math.Min(MaxDisplayedLength, log.Length);
                ImGui.Text(log.Substring(log.Length - lengthToShow, lengthToShow));
            });
        }

        private WindowRect GetDefaultPlacement()
        {
            int height = uiMgr_.BaseHeight / 4;

            return new WindowRect(uiMgr_.CenterColPosX, uiMgr_.BasePosY + uiMgr_.BaseHeight - height, uiMgr_.CenterColWidth, height);
        }
    }
}
