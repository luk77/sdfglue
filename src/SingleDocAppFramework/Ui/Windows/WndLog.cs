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

        public override string Title => "Log";

        public override void Build()
        {
            BuildWindow(delegate()
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
    }
}
