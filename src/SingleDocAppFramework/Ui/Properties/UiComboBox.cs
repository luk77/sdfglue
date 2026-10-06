//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.UndoSystem.Actions;

namespace SingleDocAppFramework.Ui.Properties
{
    public class UiComboBox
    {
        // Combo box operating on an index stored in an int value, with undo.
        // onValueChanged is called immediately after a selection (and after undo/redo).
        public static void Build(ref int id, string name, string[] names, ExInt obj, OnValueChanged? onValueChanged = null)
        {
            Build(ref id, name, names, ref obj.Val, onValueChanged);
            UndoEditTracker.HandleLastItem(() => !obj.PrevVal.Equals(obj.Val), () => new ActionInt(obj, onValueChanged));
        }

        public static void Build(ref int id, string name, string[] names, ref int val, OnValueChanged? onValueChanged = null)
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
