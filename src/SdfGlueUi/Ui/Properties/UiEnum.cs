//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;

namespace SdfGlueUi.Ui.Properties
{
    public class UiEnum
    {
        public static int Build(ref int id, string name, int enumVal, string[] enumDisplayNames)
        {
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            ImGui.Combo("", ref enumVal, enumDisplayNames, enumDisplayNames.Length);
            ImGui.PopID();
            ImGui.NextColumn();
            return enumVal;
        }
    }
}
