//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;

namespace SdfGlueUi.Ui.Properties
{
    public class UiComboBox
    {
        public static void Build(ref int id, string name, string[] names, ref int val, DataModel.OnValueChanged onValueChanged = null)
        {
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);

            int oldIndex = val;
            ImGui.PushID(id++);
            ImGui.Combo("", ref val, names, names.Length, names.Length);
            ImGui.PopID();
            if (oldIndex != val)
            {
                if (onValueChanged != null)
                    onValueChanged();
            }
            ImGui.NextColumn();
        }
    }
}
